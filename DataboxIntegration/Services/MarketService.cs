using System.Text.Json;
using DataboxIntegration.Models;

namespace DataboxIntegration.Services;

public class MarketService : BaseDataService<MarketDataset>
{
    public MarketService(HttpClient httpClient, IConfiguration configuration, ILogger<MarketService> logger)
        : base(httpClient, configuration, logger)
    {
    }

    protected override string ServiceName => "Market";

    protected override string GetApiKey()
    {
        return _configuration["ApiKeys:Marketstack"];
    }

    protected override string BuildApiUrl(params object[] parameters)
    {
        string symbols = parameters[0].ToString();
        int limit = (int)parameters[1];
        string apiKey = GetApiKey();
        return $"http://api.marketstack.com/v1/eod?access_key={apiKey}&symbols={symbols}&limit={limit}";
    }

    protected override List<MarketDataset> ParseResponse(JsonDocument jsonDoc)
    {
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

        return marketDatasets;
    }

    public async Task<List<MarketDataset>> GetMarketDataAsync(string symbols, int limit)
    {
        return await GetDataAsync(symbols, limit);
    }
}