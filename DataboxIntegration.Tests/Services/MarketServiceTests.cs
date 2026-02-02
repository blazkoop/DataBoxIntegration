using DataboxIntegration.Models;
using DataboxIntegration.Services;
using DataboxIntegration.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataboxIntegration.Tests.Services;

public class MarketServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<MarketService>> _loggerMock;

    public MarketServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<MarketService>>();

        _configurationMock.Setup(c => c["ApiKeys:Marketstack"]).Returns("test-api-key");
    }

    private MarketService CreateService(string jsonResponse)
    {
        var handler = new MockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(handler);
        return new MarketService(httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetMarketDataAsync_ValidResponse_ReturnsMarketDatasets()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": [
                {
                    ""symbol"": ""AAPL"",
                    ""date"": ""2024-01-15"",
                    ""open"": 150.50,
                    ""high"": 155.75,
                    ""low"": 149.25,
                    ""close"": 154.00,
                    ""volume"": 50000000
                }
            ]
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("AAPL", 1);

        // Assert
        Assert.Single(result);
        Assert.Equal("AAPL", result[0].Symbol);
        Assert.Equal("2024-01-15", result[0].Date);
        Assert.Equal(150.50, result[0].Open);
        Assert.Equal(155.75, result[0].High);
        Assert.Equal(149.25, result[0].Low);
        Assert.Equal(154.00, result[0].Close);
        Assert.Equal(50000000, result[0].Volume);
    }

    [Fact]
    public async Task GetMarketDataAsync_MultipleRecords_ReturnsAllDatasets()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": [
                {
                    ""symbol"": ""AAPL"",
                    ""date"": ""2024-01-15"",
                    ""open"": 150.00,
                    ""high"": 155.00,
                    ""low"": 149.00,
                    ""close"": 154.00,
                    ""volume"": 50000000
                },
                {
                    ""symbol"": ""AAPL"",
                    ""date"": ""2024-01-14"",
                    ""open"": 148.00,
                    ""high"": 151.00,
                    ""low"": 147.00,
                    ""close"": 150.00,
                    ""volume"": 45000000
                },
                {
                    ""symbol"": ""AAPL"",
                    ""date"": ""2024-01-13"",
                    ""open"": 146.00,
                    ""high"": 149.00,
                    ""low"": 145.00,
                    ""close"": 148.00,
                    ""volume"": 40000000
                }
            ]
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("AAPL", 3);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, item => Assert.Equal("AAPL", item.Symbol));
    }

    [Fact]
    public async Task GetMarketDataAsync_NullValues_DefaultsToZero()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": [
                {
                    ""symbol"": ""TEST"",
                    ""date"": ""2024-01-15"",
                    ""open"": null,
                    ""high"": null,
                    ""low"": null,
                    ""close"": null,
                    ""volume"": null
                }
            ]
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("TEST", 1);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].Open);
        Assert.Equal(0, result[0].High);
        Assert.Equal(0, result[0].Low);
        Assert.Equal(0, result[0].Close);
        Assert.Equal(0, result[0].Volume);
    }

    [Fact]
    public async Task GetMarketDataAsync_EmptyDataArray_ReturnsEmptyList()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": []
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("INVALID", 1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMarketDataAsync_GeneratesUniqueIds()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": [
                {
                    ""symbol"": ""MSFT"",
                    ""date"": ""2024-01-15"",
                    ""open"": 400.00,
                    ""high"": 405.00,
                    ""low"": 398.00,
                    ""close"": 403.00,
                    ""volume"": 30000000
                },
                {
                    ""symbol"": ""MSFT"",
                    ""date"": ""2024-01-14"",
                    ""open"": 398.00,
                    ""high"": 402.00,
                    ""low"": 396.00,
                    ""close"": 400.00,
                    ""volume"": 28000000
                }
            ]
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("MSFT", 2);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotEqual(result[0].Id, result[1].Id);
    }

    [Fact]
    public async Task GetMarketDataAsync_DecimalPrices_ParsesCorrectly()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": [
                {
                    ""symbol"": ""GOOG"",
                    ""date"": ""2024-01-15"",
                    ""open"": 141.2345,
                    ""high"": 142.9999,
                    ""low"": 140.0001,
                    ""close"": 141.5678,
                    ""volume"": 25000000
                }
            ]
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("GOOG", 1);

        // Assert
        Assert.Single(result);
        Assert.Equal(141.2345, result[0].Open, 4);
        Assert.Equal(142.9999, result[0].High, 4);
        Assert.Equal(140.0001, result[0].Low, 4);
        Assert.Equal(141.5678, result[0].Close, 4);
    }

    [Fact]
    public async Task GetMarketDataAsync_LargeVolume_ParsesCorrectly()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": [
                {
                    ""symbol"": ""NVDA"",
                    ""date"": ""2024-01-15"",
                    ""open"": 500.00,
                    ""high"": 520.00,
                    ""low"": 495.00,
                    ""close"": 515.00,
                    ""volume"": 150000000000
                }
            ]
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("NVDA", 1);

        // Assert
        Assert.Single(result);
        Assert.Equal(150000000000, result[0].Volume);
    }

    [Fact]
    public async Task GetMarketDataAsync_InvalidJson_ThrowsException()
    {
        // Arrange
        string invalidJson = "{ invalid json }";
        var service = CreateService(invalidJson);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.GetMarketDataAsync("TEST", 1));
    }

    [Fact]
    public async Task GetMarketDataAsync_OccurredAtIsValidIsoFormat()
    {
        // Arrange
        string jsonResponse = @"{
            ""data"": [
                {
                    ""symbol"": ""AMZN"",
                    ""date"": ""2024-01-15"",
                    ""open"": 180.00,
                    ""high"": 185.00,
                    ""low"": 178.00,
                    ""close"": 183.00,
                    ""volume"": 60000000
                }
            ]
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<MarketDataset> result = await service.GetMarketDataAsync("AMZN", 1);

        // Assert
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z", result[0].OccurredAt);
    }
}
