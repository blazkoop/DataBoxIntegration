using Microsoft.AspNetCore.Mvc;
using DataboxIntegration.Services;
using DataboxIntegration.Models;

namespace DataboxIntegration.Controllers;

[ApiController]
[Route("api")]
public class WeatherController : ControllerBase
{
    private readonly WeatherService _weatherService;
    private readonly DataboxService _databoxService;
    private readonly IFileLogger _fileLogger;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        WeatherService weatherService,
        DataboxService databoxService,
        IFileLogger fileLogger,
        ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _databoxService = databoxService;
        _fileLogger = fileLogger;
        _logger = logger;
    }
    
    [HttpPost("sendWeatherData/{location}")]
    public async Task<IActionResult> SendWeatherData(string location)
    {
        try
        {
            _logger.LogInformation("=== Starting sendWeatherData for {Location} ===", location);
            
            // Step 1: Get weather data from Weatherstack
            _logger.LogInformation("Step 1: Getting weather data...");
            List<WeatherDataset> weatherData = await _weatherService.GetWeatherDataAsync(location);
            
            if (weatherData == null || weatherData.Count == 0)
            {
                _fileLogger.LogDataSend("Weatherstack", 0, 0, false, "Failed to fetch weather data");
                
                ErrorResponse errorResponse = new ErrorResponse 
                { 
                    Error = "Failed to fetch weather data" 
                };
                return BadRequest(errorResponse);
            }
            
            _logger.LogInformation("Weather data retrieved: {Count} record(s)", weatherData.Count);
            
            // Step 2: Data is already parsed into WeatherDataset model
            _logger.LogInformation("Step 2: Data parsed into dataset format");
            
            // Step 3: Send to Databox using Ingestion API
            _logger.LogInformation("Step 3: Sending to Databox...");
            bool success = await _databoxService.SendWeatherDataAsync(weatherData);
            
            // Log the data send operation
            int columns = 6; // id, location, temperature, humidity, weather_description, occurredAt
            _fileLogger.LogDataSend("Weatherstack", weatherData.Count, columns, success, success ? null : "Failed to send to Databox");
            
            if (success)
            {
                _logger.LogInformation("=== sendWeatherData completed successfully ===");
                
                SendWeatherDataResponse response = new SendWeatherDataResponse
                {
                    Success = true,
                    Message = "Weather data sent to Databox successfully",
                    Data = weatherData
                };
                
                return Ok(response);
            }
            else
            {
                ErrorResponse errorResponse = new ErrorResponse 
                { 
                    Error = "Failed to send data to Databox" 
                };
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sendWeatherData");
            _fileLogger.LogDataSend("Weatherstack", 0, 0, false, ex.Message);
            
            ErrorResponse errorResponse = new ErrorResponse 
            { 
                Error = ex.Message 
            };
            return StatusCode(500, errorResponse);
        }
    }
    
    /// <summary>
    /// Preview weather data without sending to Databox
    /// </summary>
    [HttpGet("weather/preview/{location}")]
    public async Task<IActionResult> PreviewWeatherData(string location)
    {
        try
        {
            List<WeatherDataset> weatherData = await _weatherService.GetWeatherDataAsync(location);
            return Ok(weatherData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing weather data");
            
            ErrorResponse errorResponse = new ErrorResponse 
            { 
                Error = ex.Message 
            };
            return StatusCode(500, errorResponse);
        }
    }
}