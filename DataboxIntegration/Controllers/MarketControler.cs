using Microsoft.AspNetCore.Mvc;
using DataboxIntegration.Services;
using DataboxIntegration.Models;

namespace DataboxIntegration.Controllers;

[ApiController]
[Route("api")]
public class MarketController : ControllerBase
{
    private readonly MarketService _marketService;
    private readonly DataboxService _databoxService;
    private readonly IFileLogger _fileLogger;
    private readonly ILogger<MarketController> _logger;

    public MarketController(
        MarketService marketService,
        DataboxService databoxService,
        IFileLogger fileLogger,
        ILogger<MarketController> logger)
    {
        _marketService = marketService;
        _databoxService = databoxService;
        _fileLogger = fileLogger;
        _logger = logger;
    }

    /// <summary>
    /// Send market data to Databox
    /// Flow: Get market data -> Parse into dataset format -> Send to Databox
    /// </summary>
    [HttpPost("sendMarketData/{symbols}")]
    public async Task<IActionResult> SendMarketData(string symbols, [FromQuery] int limit = 5)
    {
        try
        {
            _logger.LogInformation("=== Starting sendMarketData for {Symbols} ===", symbols);
            
            // Step 1: Get market data from Marketstack
            _logger.LogInformation("Step 1: Getting market data...");
            List<MarketDataset> marketData = await _marketService.GetMarketDataAsync(symbols, limit);
            
            if (marketData == null || marketData.Count == 0)
            {
                _fileLogger.LogDataSend("Marketstack", 0, 0, false, "Failed to fetch market data");
                
                ErrorResponse errorResponse = new ErrorResponse 
                { 
                    Error = "Failed to fetch market data" 
                };
                return BadRequest(errorResponse);
            }
            
            _logger.LogInformation("Market data retrieved: {Count} records", marketData.Count);
            
            // Step 2: Data is already parsed into MarketDataset model
            _logger.LogInformation("Step 2: Data parsed into dataset format");
            
            // Step 3: Send to Databox using Ingestion API
            _logger.LogInformation("Step 3: Sending to Databox...");
            bool success = await _databoxService.SendMarketDataAsync(marketData);
            
            // Log the data send operation
            int columns = 9; // id, symbol, date, open, high, low, close, volume, occurredAt
            _fileLogger.LogDataSend("Marketstack", marketData.Count, columns, success, success ? null : "Failed to send to Databox");
            
            if (success)
            {
                _logger.LogInformation("=== sendMarketData completed successfully ===");
                
                SendMarketDataResponse response = new SendMarketDataResponse
                {
                    Success = true,
                    Message = "Market data sent to Databox successfully",
                    Data = marketData
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
            _logger.LogError(ex, "Error in sendMarketData");
            _fileLogger.LogDataSend("Marketstack", 0, 0, false, ex.Message);
            
            ErrorResponse errorResponse = new ErrorResponse 
            { 
                Error = ex.Message 
            };
            return StatusCode(500, errorResponse);
        }
    }
    
    /// <summary>
    /// Preview market data without sending to Databox
    /// </summary>
    [HttpGet("market/preview/{symbols}")]
    public async Task<IActionResult> PreviewMarketData(string symbols, [FromQuery] int limit = 5)
    {
        try
        {
            List<MarketDataset> marketData = await _marketService.GetMarketDataAsync(symbols, limit);
            return Ok(marketData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing market data");
            
            ErrorResponse errorResponse = new ErrorResponse 
            { 
                Error = ex.Message 
            };
            return StatusCode(500, errorResponse);
        }
    }
}