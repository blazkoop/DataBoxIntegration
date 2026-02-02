using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataboxIntegration.Services;

/// <summary>
/// Abstract base class for external data services
/// Provides common functionality for API calls and data parsing
/// </summary>
public abstract class BaseDataService<TDataset>
{
    protected readonly HttpClient _httpClient;
    protected readonly IConfiguration _configuration;
    protected readonly ILogger _logger;

    protected BaseDataService(HttpClient httpClient, IConfiguration configuration, ILogger logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }
    
    protected abstract string GetApiKey();
    
    protected abstract string BuildApiUrl(params object[] parameters);
    
    protected abstract List<TDataset> ParseResponse(JsonDocument jsonDoc);
    
    protected abstract string ServiceName { get; }
    
    public async Task<List<TDataset>> GetDataAsync(params object[] parameters)
    {
        List<TDataset> datasets = new List<TDataset>();

        try
        {
            string apiKey = GetApiKey();
            string url = BuildApiUrl(parameters);

            _logger.LogInformation("Fetching {ServiceName} data from: {Url}", ServiceName, url);

            string response = await _httpClient.GetStringAsync(url);
            JsonDocument jsonDoc = JsonDocument.Parse(response);

            datasets = ParseResponse(jsonDoc);

            _logger.LogInformation("Successfully fetched {Count} {ServiceName} records", datasets.Count, ServiceName);
            return datasets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {ServiceName} data", ServiceName);
            throw;
        }
    }
}