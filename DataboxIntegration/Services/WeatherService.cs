using System.Text.Json;
using DataboxIntegration.Models;

namespace DataboxIntegration.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<WeatherDataset>> GetWeatherDataAsync(string location)
    {
        try
        {
            string apiKey = _configuration["ApiKeys:Weatherstack"];
            string url = $"http://api.weatherstack.com/current?access_key={apiKey}&query={location}";

            _logger.LogInformation("Fetching weather data from: {Url}", url);

            string response = await _httpClient.GetStringAsync(url);
            JsonDocument jsonDoc = JsonDocument.Parse(response);

            JsonElement locationElement = jsonDoc.RootElement.GetProperty("location");
            string locationName = locationElement.GetProperty("name").GetString() ?? "";

            JsonElement currentElement = jsonDoc.RootElement.GetProperty("current");
            int temperature = currentElement.GetProperty("temperature").GetInt32();
            int humidity = currentElement.GetProperty("humidity").GetInt32();
            string weatherDescription = currentElement.GetProperty("weather_descriptions")[0].GetString() ?? "";

            WeatherDataset weatherDataset = new WeatherDataset
            {
                Id = Guid.NewGuid().ToString(),
                Location = locationName,
                Temperature = temperature,
                Humidity = humidity,
                WeatherDescription = weatherDescription,
                OccurredAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")
            };

            _logger.LogInformation("Successfully fetched weather data for {Location}", locationName);

            return new List<WeatherDataset> { weatherDataset };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data");
            throw;
        }
    }
}
