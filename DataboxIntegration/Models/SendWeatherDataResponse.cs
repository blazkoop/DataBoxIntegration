namespace DataboxIntegration.Models;

/// <summary>
/// API response for sendWeatherData endpoint
/// </summary>
public class SendWeatherDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<WeatherDataset> Data { get; set; }
}