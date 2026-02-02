using System.Net;
using DataboxIntegration.Models;
using DataboxIntegration.Services;
using DataboxIntegration.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataboxIntegration.Tests.Services;

public class DataboxServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<DataboxService>> _loggerMock;

    public DataboxServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<DataboxService>>();

        _configurationMock.Setup(c => c["ApiKeys:Databox"]).Returns("test-api-key");
        _configurationMock.Setup(c => c["Databox:WeatherDatasetId"]).Returns("weather-dataset-123");
        _configurationMock.Setup(c => c["Databox:MarketDatasetId"]).Returns("market-dataset-456");
    }

    private DataboxService CreateService(HttpStatusCode statusCode, string responseContent = "{}")
    {
        var handler = new MockHttpMessageHandler(responseContent, statusCode);
        var httpClient = new HttpClient(handler);
        return new DataboxService(httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    private DataboxService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        var handler = new MockHttpMessageHandler(handlerFunc);
        var httpClient = new HttpClient(handler);
        return new DataboxService(httpClient, _configurationMock.Object, _loggerMock.Object);
    }
    
    [Fact]
    public async Task SendWeatherDataAsync_SuccessfulResponse_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(HttpStatusCode.OK, @"{""status"": ""success""}");
        var weatherData = new List<WeatherDataset>
        {
            new WeatherDataset
            {
                Id = "test-id",
                Location = "New York",
                Temperature = 22,
                Humidity = 65,
                WeatherDescription = "Sunny",
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        bool result = await service.SendWeatherDataAsync(weatherData);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendWeatherDataAsync_FailedResponse_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(HttpStatusCode.BadRequest, @"{""error"": ""Invalid request""}");
        var weatherData = new List<WeatherDataset>
        {
            new WeatherDataset
            {
                Id = "test-id",
                Location = "London",
                Temperature = 15,
                Humidity = 70,
                WeatherDescription = "Cloudy",
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        bool result = await service.SendWeatherDataAsync(weatherData);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendWeatherDataAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(HttpStatusCode.InternalServerError, @"{""error"": ""Server error""}");
        var weatherData = new List<WeatherDataset>
        {
            new WeatherDataset
            {
                Id = "test-id",
                Location = "Paris",
                Temperature = 18,
                Humidity = 55,
                WeatherDescription = "Clear",
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        bool result = await service.SendWeatherDataAsync(weatherData);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendWeatherDataAsync_MissingDatasetId_ReturnsFalse()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ApiKeys:Databox"]).Returns("test-api-key");
        configMock.Setup(c => c["Databox:WeatherDatasetId"]).Returns((string?)null);

        var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = new DataboxService(httpClient, configMock.Object, _loggerMock.Object);

        var weatherData = new List<WeatherDataset>
        {
            new WeatherDataset
            {
                Id = "test-id",
                Location = "Berlin",
                Temperature = 10,
                Humidity = 60,
                WeatherDescription = "Rainy",
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        bool result = await service.SendWeatherDataAsync(weatherData);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendWeatherDataAsync_MultipleRecords_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(HttpStatusCode.OK, @"{""status"": ""success""}");
        var weatherData = new List<WeatherDataset>
        {
            new WeatherDataset { Id = "id-1", Location = "New York", Temperature = 22, Humidity = 65, WeatherDescription = "Sunny", OccurredAt = "2024-01-15T12:00:00.000000Z" },
            new WeatherDataset { Id = "id-2", Location = "London", Temperature = 15, Humidity = 70, WeatherDescription = "Cloudy", OccurredAt = "2024-01-15T12:00:00.000000Z" },
            new WeatherDataset { Id = "id-3", Location = "Tokyo", Temperature = 18, Humidity = 50, WeatherDescription = "Clear", OccurredAt = "2024-01-15T12:00:00.000000Z" }
        };

        // Act
        bool result = await service.SendWeatherDataAsync(weatherData);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendWeatherDataAsync_SendsCorrectPayload()
    {
        // Arrange
        string? capturedContent = null;
        string? capturedUrl = null;

        var service = CreateService(request =>
        {
            capturedUrl = request.RequestUri?.ToString();
            capturedContent = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""status"": ""success""}")
            };
        });

        var weatherData = new List<WeatherDataset>
        {
            new WeatherDataset
            {
                Id = "test-123",
                Location = "Seattle",
                Temperature = 12,
                Humidity = 80,
                WeatherDescription = "Rainy",
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        await service.SendWeatherDataAsync(weatherData);

        // Assert
        Assert.NotNull(capturedContent);
        Assert.Contains("\"id\"", capturedContent);
        Assert.Contains("\"location\"", capturedContent);
        Assert.Contains("\"temperature\"", capturedContent);
        Assert.Contains("\"humidity\"", capturedContent);
        Assert.Contains("\"weather_description\"", capturedContent);
        Assert.Contains("\"occurredAt\"", capturedContent);
        Assert.Contains("Seattle", capturedContent);
        Assert.Equal("https://api.databox.com/v1/datasets/weather-dataset-123/data", capturedUrl);
    }
    
    [Fact]
    public async Task SendMarketDataAsync_SuccessfulResponse_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(HttpStatusCode.OK, @"{""status"": ""success""}");
        var marketData = new List<MarketDataset>
        {
            new MarketDataset
            {
                Id = "test-id",
                Symbol = "AAPL",
                Date = "2024-01-15",
                Open = 150.50,
                High = 155.75,
                Low = 149.25,
                Close = 154.00,
                Volume = 50000000,
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        bool result = await service.SendMarketDataAsync(marketData);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendMarketDataAsync_FailedResponse_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(HttpStatusCode.BadRequest, @"{""error"": ""Invalid request""}");
        var marketData = new List<MarketDataset>
        {
            new MarketDataset
            {
                Id = "test-id",
                Symbol = "MSFT",
                Date = "2024-01-15",
                Open = 400.00,
                High = 405.00,
                Low = 398.00,
                Close = 403.00,
                Volume = 30000000,
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        bool result = await service.SendMarketDataAsync(marketData);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendMarketDataAsync_MissingDatasetId_ReturnsFalse()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ApiKeys:Databox"]).Returns("test-api-key");
        configMock.Setup(c => c["Databox:MarketDatasetId"]).Returns((string?)null);

        var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = new DataboxService(httpClient, configMock.Object, _loggerMock.Object);

        var marketData = new List<MarketDataset>
        {
            new MarketDataset
            {
                Id = "test-id",
                Symbol = "GOOG",
                Date = "2024-01-15",
                Open = 140.00,
                High = 145.00,
                Low = 138.00,
                Close = 143.00,
                Volume = 25000000,
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        bool result = await service.SendMarketDataAsync(marketData);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendMarketDataAsync_MultipleRecords_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(HttpStatusCode.OK, @"{""status"": ""success""}");
        var marketData = new List<MarketDataset>
        {
            new MarketDataset { Id = "id-1", Symbol = "AAPL", Date = "2024-01-15", Open = 150.00, High = 155.00, Low = 149.00, Close = 154.00, Volume = 50000000, OccurredAt = "2024-01-15T12:00:00.000000Z" },
            new MarketDataset { Id = "id-2", Symbol = "AAPL", Date = "2024-01-14", Open = 148.00, High = 151.00, Low = 147.00, Close = 150.00, Volume = 45000000, OccurredAt = "2024-01-15T12:00:00.000000Z" },
            new MarketDataset { Id = "id-3", Symbol = "AAPL", Date = "2024-01-13", Open = 146.00, High = 149.00, Low = 145.00, Close = 148.00, Volume = 40000000, OccurredAt = "2024-01-15T12:00:00.000000Z" }
        };

        // Act
        bool result = await service.SendMarketDataAsync(marketData);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendMarketDataAsync_SendsCorrectPayload()
    {
        // Arrange
        string? capturedContent = null;
        string? capturedUrl = null;

        var service = CreateService(request =>
        {
            capturedUrl = request.RequestUri?.ToString();
            capturedContent = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""status"": ""success""}")
            };
        });

        var marketData = new List<MarketDataset>
        {
            new MarketDataset
            {
                Id = "test-456",
                Symbol = "NVDA",
                Date = "2024-01-15",
                Open = 500.00,
                High = 520.00,
                Low = 495.00,
                Close = 515.00,
                Volume = 100000000,
                OccurredAt = "2024-01-15T12:00:00.000000Z"
            }
        };

        // Act
        await service.SendMarketDataAsync(marketData);

        // Assert
        Assert.NotNull(capturedContent);
        Assert.Contains("\"id\"", capturedContent);
        Assert.Contains("\"symbol\"", capturedContent);
        Assert.Contains("\"date\"", capturedContent);
        Assert.Contains("\"open\"", capturedContent);
        Assert.Contains("\"high\"", capturedContent);
        Assert.Contains("\"low\"", capturedContent);
        Assert.Contains("\"close\"", capturedContent);
        Assert.Contains("\"volume\"", capturedContent);
        Assert.Contains("\"occurredAt\"", capturedContent);
        Assert.Contains("NVDA", capturedContent);
        Assert.Equal("https://api.databox.com/v1/datasets/market-dataset-456/data", capturedUrl);
    }

}
