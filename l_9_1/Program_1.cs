//using System;
//using System.IO;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Threading;
//using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

class Program_1
{
    static readonly Mutex mutex = new Mutex();
    static readonly string api = "R20zc09NTVJfREo1QTBsWENuaFhWcF9zSlBQRTY1WkwwRE1hUm1WMGw3VT0";
    static async Task Main()
    {
        List<string> tickers = new List<string>();
        using (StreamReader reader = new StreamReader("ticker.txt"))
        {
            string line;
            while ((line = reader.ReadLine()) != null) { tickers.Add(line); }
        }
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {api}");
            List<Task> tasks = new List<Task>();
            foreach (string ticker in tickers) { tasks.Add(GetDataForTicker(client, ticker)); }
            await Task.WhenAll(tasks);
        }
    }
    static async Task GetDataForTicker(HttpClient client, string ticker)
    {
        try
        {
            DateTime startDate = DateTime.Now.AddYears(-1);
            DateTime endDate = DateTime.Now;
            string fromDate = startDate.ToString("2020-01-01");
            string toDate = endDate.ToString("2020-01-02");
            string url = $"https://api.marketdata.app/v1/stocks/candles/D/{ticker}?from={fromDate}&to={toDate}";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string jsonData = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonData);
            if (data?.data == null)
            {
                Console.WriteLine($"No data found for {ticker}.");
                return;
            }
            double totalAveragePrice = 0.0;
            int totalRowCount = 0;
            foreach (var candle in data.data)
            {
                try
                {
                    double high = (double)candle.highle.high;
                    double low = (double)candle.low;
                    double averagePrice = (high + low) / 2;
                    totalAveragePrice += averagePrice;
                    totalRowCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error when processing candle for {ticker}: {ex.Message}");
                }
            }
            if (totalRowCount > 0)
            {
                double totalAverage = totalAveragePrice / totalRowCount;
                string result = $"{ticker}:{totalAverage}";
                mutex.WaitOne();
                try { File.AppendAllText("results.txt", result + Environment.NewLine); }
                finally { mutex.ReleaseMutex(); }
                Console.WriteLine($"The average price for {ticker} for a year: {totalAverage}");
            }
            else { Console.WriteLine($"For {ticker} there are no data for that year."); }
        }
        catch (Exception ex) { Console.WriteLine($"Error when processing {ticker}: {ex.Message}"); }
    }
}
