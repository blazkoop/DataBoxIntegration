using System.Text.Json;
using DataboxIntegration.Models;

namespace DataboxIntegration.Services;

public class WeatherService : BaseDataService<WeatherDataset>
{
    public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger) : base(httpClient, configuration, logger) { }

    protected override string ServiceName => "Weather";

    protected override string GetApiKey()
    {
        return _configuration["ApiKeys:Weatherstack"];
    }

    protected override string BuildApiUrl(params object[] parameters)
    {
        string location = parameters[0].ToString();
        string apiKey = GetApiKey();
        return $"http://api.weatherstack.com/current?access_key={apiKey}&query={location}";
    }

    protected override List<WeatherDataset> ParseResponse(JsonDocument jsonDoc)
    {
        List<WeatherDataset> weatherDatasets = new List<WeatherDataset>();

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

        weatherDatasets.Add(weatherDataset);
        return weatherDatasets;
    }

    public async Task<List<WeatherDataset>> GetWeatherDataAsync(string location)
    {
        return await GetDataAsync(location);
    }
}