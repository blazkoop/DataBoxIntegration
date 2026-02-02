namespace DataboxIntegration.Models;

/// <summary>
/// API response for sendMarketData endpoint
/// </summary>
public class SendMarketDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<MarketDataset> Data { get; set; }
}