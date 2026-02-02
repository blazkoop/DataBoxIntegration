using System.Text;
using System.Text.Json;
using DataboxIntegration.Models;

namespace DataboxIntegration.Services;

public class DataboxService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataboxService> _logger;

    public DataboxService(HttpClient httpClient, IConfiguration configuration, ILogger<DataboxService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        // Set up headers
        string apiKey = _configuration["ApiKeys:Databox"];
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
    }

    public async Task<bool> SendWeatherDataAsync(List<WeatherDataset> weatherData)
    {
        try
        {
            string datasetId = _configuration["Databox:WeatherDatasetId"];
            
            if (string.IsNullOrEmpty(datasetId))
            {
                _logger.LogError("Weather dataset ID not configured");
                return false;
            }
            
            // Convert models to dictionaries for Databox API
            List<Dictionary<string, object>> records = new List<Dictionary<string, object>>();
            
            foreach (WeatherDataset weather in weatherData)
            {
                Dictionary<string, object> record = new Dictionary<string, object>
                {
                    { "id", weather.Id },
                    { "location", weather.Location },
                    { "temperature", weather.Temperature },
                    { "humidity", weather.Humidity },
                    { "weather_description", weather.WeatherDescription },
                    { "occurredAt", weather.OccurredAt }
                };
                
                records.Add(record);
            }
            
            // Create request payload
            object payload = new
            {
                records = records
            };
            
            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Sending {Count} weather record(s) to Databox:\n{Json}", records.Count, json);
            
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(
                $"https://api.databox.com/v1/datasets/{datasetId}/data",
                content
            );
            
            string responseBody = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent weather data to Databox. Response: {Response}", responseBody);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send weather data. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending weather data to Databox");
            return false;
        }
    }

    public async Task<bool> SendMarketDataAsync(List<MarketDataset> marketData)
    {
        try
        {
            string datasetId = _configuration["Databox:MarketDatasetId"];
            
            if (string.IsNullOrEmpty(datasetId))
            {
                _logger.LogError("Market dataset ID not configured");
                return false;
            }
            
            // Convert models to dictionaries for Databox API
            List<Dictionary<string, object>> records = new List<Dictionary<string, object>>();
            
            foreach (MarketDataset market in marketData)
            {
                Dictionary<string, object> record = new Dictionary<string, object>
                {
                    { "id", market.Id },
                    { "symbol", market.Symbol },
                    { "date", market.Date },
                    { "open", market.Open },
                    { "high", market.High },
                    { "low", market.Low },
                    { "close", market.Close },
                    { "volume", market.Volume },
                    { "occurredAt", market.OccurredAt }
                };
                
                records.Add(record);
            }
            
            // Create request payload
            object payload = new
            {
                records = records
            };
            
            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Sending {Count} market records to Databox:\n{Json}", records.Count, json);
            
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(
                $"https://api.databox.com/v1/datasets/{datasetId}/data",
                content
            );
            
            string responseBody = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent market data to Databox. Response: {Response}", responseBody);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send market data. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending market data to Databox");
            return false;
        }
    }
}