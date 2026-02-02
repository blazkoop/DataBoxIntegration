using System.Text.Json;
using DataboxIntegration.Models;

namespace DataboxIntegration.Services;

public class MarketService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MarketService> _logger;

    public MarketService(HttpClient httpClient, IConfiguration configuration, ILogger<MarketService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<MarketDataset>> GetMarketDataAsync(string symbols, int limit)
    {
        try
        {
            string apiKey = _configuration["ApiKeys:Marketstack"];
            string url = $"http://api.marketstack.com/v1/eod?access_key={apiKey}&symbols={symbols}&limit={limit}";

            _logger.LogInformation("Fetching market data from: {Url}", url);

            string response = await _httpClient.GetStringAsync(url);
            JsonDocument jsonDoc = JsonDocument.Parse(response);

            List<MarketDataset> marketDatasets = new List<MarketDataset>();
            JsonElement dataArray = jsonDoc.RootElement.GetProperty("data");

            foreach (JsonElement item in dataArray.EnumerateArray())
            {
                string symbol = item.GetProperty("symbol").GetString() ?? "";
                string date = item.GetProperty("date").GetString() ?? "";

                double open = item.GetProperty("open").ValueKind == JsonValueKind.Null
                    ? 0
                    : (double)item.GetProperty("open").GetDecimal();

                double high = item.GetProperty("high").ValueKind == JsonValueKind.Null
                    ? 0
                    : (double)item.GetProperty("high").GetDecimal();

                double low = item.GetProperty("low").ValueKind == JsonValueKind.Null
                    ? 0
                    : (double)item.GetProperty("low").GetDecimal();

                double close = item.GetProperty("close").ValueKind == JsonValueKind.Null
                    ? 0
                    : (double)item.GetProperty("close").GetDecimal();

                long volume = item.GetProperty("volume").ValueKind == JsonValueKind.Number
                    ? (long)item.GetProperty("volume").GetDouble()
                    : 0;

                MarketDataset marketDataset = new MarketDataset
                {
                    Id = Guid.NewGuid().ToString(),
                    Symbol = symbol,
                    Date = date,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                    OccurredAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")
                };

                marketDatasets.Add(marketDataset);
            }

            _logger.LogInformation("Successfully fetched {Count} market records", marketDatasets.Count);

            return marketDatasets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching market data");
            throw;
        }
    }
}
