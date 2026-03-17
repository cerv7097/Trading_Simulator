using System.Text.Json;
using BlazorTradingApp.Models;
using Microsoft.Extensions.Configuration;

namespace BlazorTradingApp.Services
{
    public class TwelveDataService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public TwelveDataService(IConfiguration config, HttpClient httpClient)
        {
            _apiKey = config["TwelveData:ApiKey"] ?? throw new InvalidOperationException("TwelveData:ApiKey not configured.");
            _httpClient = httpClient;
        }

        public async Task<List<StockPrice>> GetPricesAsync(string symbol, string interval = "1h", int outputSize = 100)
        {
            var url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval={interval}&outputsize={outputSize}&apikey={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API error {response.StatusCode}: {errorText}");
                throw new Exception("Failed to retrieve stock data.");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("values", out var values))
                throw new Exception("No 'values' in response — check symbol or API limit.");

            var prices = new List<StockPrice>();

            foreach (var item in values.EnumerateArray().Reverse())
            {
                prices.Add(new StockPrice
                {
                    Date   = DateTime.Parse(item.GetProperty("datetime").GetString()!),
                    Open   = decimal.Parse(item.GetProperty("open").GetString()!),
                    High   = decimal.Parse(item.GetProperty("high").GetString()!),
                    Low    = decimal.Parse(item.GetProperty("low").GetString()!),
                    Close  = decimal.Parse(item.GetProperty("close").GetString()!),
                    Volume = long.TryParse(item.GetProperty("volume").GetString(), out var vol) ? vol : 0
                });
            }

            return prices;
        }
    }
}
