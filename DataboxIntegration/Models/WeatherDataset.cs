namespace DataboxIntegration.Models;

/// <summary>
/// Weather data formatted for Databox dataset
/// </summary>
public class WeatherDataset
{
    public string Id { get; set; }
    public string Location { get; set; }
    public int Temperature { get; set; }
    public int Humidity { get; set; }
    public string WeatherDescription { get; set; }
    public string OccurredAt { get; set; }
}
