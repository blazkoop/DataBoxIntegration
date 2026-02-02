namespace DataboxIntegration.Models;

public class MarketDataset
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public string Date { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public long Volume { get; set; }
    public string OccurredAt { get; set; }
}