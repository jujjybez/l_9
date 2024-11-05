using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeatherApp
{
    public partial class MainForm : Form
    {
        private List<City> cities = new List<City>();
        private string api_key = "5cc53f61b2c0f1891c6bbb11b670a9f4";
        private ListBox listBoxCities;
        private Button buttonGetWeather;
        private Label labelWeatherInfo;
        private string URL = "https://api.openweathermap.org/data/2.5/weather";

        public MainForm()
        {
            InitializeComponent();
            LoadCities();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadCities();
        }

        private async Task LoadCities()
        {
            try
            {
                var cities = await ReadCitiesFromFileAsync("city.txt");
                listBoxCities.Items.AddRange(cities.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading city data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonGetWeather_Click(object sender, EventArgs e)
        {
                if (listBoxCities.SelectedItem == null)
                {
                    MessageBox.Show("Choose city", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var selectedCity = listBoxCities.SelectedItem.ToString();
                var coordinates = selectedCity.Split(',');
            if (coordinates.Length != 3)
            {
                MessageBox.Show("Invalid city data", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            double latitude = Convert.ToDouble(coordinates[1], new System.Globalization.CultureInfo("en-US"));
            double longitude = Convert.ToDouble(coordinates[2], new System.Globalization.CultureInfo("en-US"));
            var weather = await GetWeatherAsync(latitude, longitude);
            if (weather != null)
            {
                DisplayWeatherInfo(weather);
            }
            else
            {
                labelWeatherInfo.Text = "Failed to fetch weather data";
            }
            //Refresh();
        }

        private async Task<List<string>> ReadCitiesFromFileAsync(string filePath)
        {
            try
            {
                List<string> cities = new List<string>();

                using (StreamReader reader = new StreamReader(filePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = await reader.ReadLineAsync();
                        string[] parts = line.Split('\t');
                        if (parts.Length == 2)
                        {
                            string cityName = parts[0].Trim();
                            string coordinates = parts[1].Trim();
                            if (IsValidCoordinates(coordinates))
                            {
                                cities.Add($"{cityName},{coordinates}");
                            }
                            else
                            {
                                MessageBox.Show($"Invalid coordinates format in line: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Invalid line format: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                return cities;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading cities from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<string>();
            }
        }

        private bool IsValidCoordinates(string coordinates)
        {
            if (Regex.IsMatch(coordinates, @"^\s*-?\d+(\.\d+)?,\s*-?\d+(\.\d+)?\s*$"))
            {
                return true;
            }
            return false;
        }

        private async Task<Weather> GetWeatherAsync(double latitude, double longitude)
        {
            using (HttpClient client = new HttpClient())
            {
                int maxAttempts = 10;
                int attempt = 0;
                while (attempt < maxAttempts)
                {
                    try
                    {
                        var response = await client.GetStringAsync($"{URL}?lat={latitude}&lon={longitude}&appid={api_key}&units=metric");
                        var weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(response);
                        if (weatherInfo != null && !string.IsNullOrEmpty(weatherInfo.sys.country))
                        {
                            string country = weatherInfo.sys.country;
                            string name = weatherInfo.name;
                            double temp = weatherInfo.main.temp;
                            string description = weatherInfo.weather[0].description;
                            return new Weather
                            {
                                Country = country,
                                Name = name,
                                Temp = temp,
                                Description = description
                            };
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"HTTP request error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        attempt++;
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"JSON deserialization error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        attempt++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        attempt++;
                    }
                }

                return null;
            }
        }

        private void DisplayWeatherInfo(Weather weather)
        {
            if (weather != null)
            {
                string weatherInfoText = $"Country: {weather.Country}\nCity: {weather.Name}\nTemperature: {weather.Temp}°C\nDescription: {weather.Description}";
                labelWeatherInfo.Text = weatherInfoText;
            }
            else
            {
                labelWeatherInfo.Text = "Weather data not available.";
            }
        }

        public class City
        {
            public string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public class Weather
        {
            public string Country { get; set; }
            public string Name { get; set; }
            public double Temp { get; set; }
            public string Description { get; set; }
        }

        public class WeatherInfo
        {
            public MainInfo main { get; set; }
            public WeatherDescription[] weather { get; set; }
            public string name { get; set; }
            public SysInfo sys { get; set; }
        }

        public class MainInfo
        {
            public double temp { get; set; }
        }

        public class WeatherDescription
        {
            public string description { get; set; }
        }

        public class SysInfo
        {
            public string country { get; set; }
        }


        private void InitializeComponent()
        {
            this.listBoxCities = new System.Windows.Forms.ListBox();
            this.buttonGetWeather = new System.Windows.Forms.Button();
            this.labelWeatherInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listBoxCities
            // 
            this.listBoxCities.FormattingEnabled = true;
            this.listBoxCities.ItemHeight = 20;
            this.listBoxCities.Location = new System.Drawing.Point(73, 12);
            this.listBoxCities.Name = "listBoxCities";
            this.listBoxCities.Size = new System.Drawing.Size(120, 84);
            this.listBoxCities.TabIndex = 0;
            // 
            // buttonGetWeather
            // 
            this.buttonGetWeather.Location = new System.Drawing.Point(98, 133);
            this.buttonGetWeather.Name = "buttonGetWeather";
            this.buttonGetWeather.Size = new System.Drawing.Size(75, 23);
            this.buttonGetWeather.TabIndex = 1;
            this.buttonGetWeather.Text = "Get Weather";
            this.buttonGetWeather.UseVisualStyleBackColor = true;
            this.buttonGetWeather.Click += new EventHandler(buttonGetWeather_Click);
            // 
            // labelWeatherInfo
            // 
            this.labelWeatherInfo.Location = new System.Drawing.Point(73, 170);
            this.labelWeatherInfo.Size = new System.Drawing.Size(200, 60);

            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Controls.Add(this.buttonGetWeather);
            this.Controls.Add(this.listBoxCities);
            this.Name = "MainForm";
            this.ResumeLayout(false);

        }
    }
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}







/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace WeatherApp
{
    public partial class MainForm : Form
    {
        private List<City> cities = new List<City>();
        private string api_key = "5cc53f61b2c0f1891c6bbb11b670a9f4";
        private ListBox listBoxCities;
        private Button buttonGetWeather;
        private string URL = "https://api.openweathermap.org/data/2.5/weather";

        public MainForm()
        {
            InitializeComponent();
            LoadCities();
        }

        private void LoadCities()
        {
            try
            {
                var lines = File.ReadAllLines("city.txt");
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 3)
                    {
                        cities.Add(new City
                        {
                            Name = parts[0].Trim(),
                            Latitude = double.Parse(parts[1].Trim()),
                            Longitude = double.Parse(parts[2].Trim())
                        });
                    }
                }
                listBoxCities.DataSource = cities.Select(c => c.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading cities: " + ex.Message);
            }
        }

        private async void buttonGetWeather_Click(object sender, EventArgs e)
        {
            try
            {
                if (listBoxCities.SelectedItem != null)
                {
                    var selectedCity = cities.First(c => c.Name == listBoxCities.SelectedItem.ToString());
                    await GetWeatherAsync(selectedCity);
                }
                else
                {
                    MessageBox.Show("Error.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async Task GetWeatherAsync(City city)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(URL);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string urlParameters = $"?lat={city.Latitude}&lon={city.Longitude}&appid={api_key}";

                try
                {
                    HttpResponseMessage response = await client.GetAsync(urlParameters);
                    if (response.IsSuccessStatusCode)
                    {
                        var data_obj = await response.Content.ReadAsStringAsync();
                        JObject json = JObject.Parse(data_obj);
                        double temp = (double)json["main"]["temp"];
                        string description = (string)json["weather"][0]["description"];
                        string country = (string)json["sys"]["country"];
                        MessageBox.Show($"Weather in {city.Name}:\nTemperature: {temp}K\nDescription: {description}");
                    }
                    else
                    {
                        MessageBox.Show($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error fetching weather data: " + ex.Message);
                }
            }
        }

        public class City
        {
            public string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        private void InitializeComponent()
        {
            this.listBoxCities = new System.Windows.Forms.ListBox();
            this.buttonGetWeather = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBoxCities
            // 
            this.listBoxCities.FormattingEnabled = true;
            this.listBoxCities.ItemHeight = 20;
            this.listBoxCities.Location = new System.Drawing.Point(73, 12);
            this.listBoxCities.Name = "listBoxCities";
            this.listBoxCities.Size = new System.Drawing.Size(120, 84);
            this.listBoxCities.TabIndex = 0;
            // 
            // buttonGetWeather
            // 
            this.buttonGetWeather.Location = new System.Drawing.Point(98, 133);
            this.buttonGetWeather.Name = "buttonGetWeather";
            this.buttonGetWeather.Size = new System.Drawing.Size(75, 23);
            this.buttonGetWeather.TabIndex = 1;
            this.buttonGetWeather.Text = "buttonGetWeather";
            this.buttonGetWeather.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Controls.Add(this.buttonGetWeather);
            this.Controls.Add(this.listBoxCities);
            this.Name = "MainForm";
            this.ResumeLayout(false);

        }
    }
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
*/