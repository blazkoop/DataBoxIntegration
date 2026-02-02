using DataboxIntegration.Models;
using DataboxIntegration.Services;
using DataboxIntegration.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataboxIntegration.Tests.Services;

public class WeatherServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<WeatherService>> _loggerMock;

    public WeatherServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<WeatherService>>();

        _configurationMock.Setup(c => c["ApiKeys:Weatherstack"]).Returns("test-api-key");
    }

    private WeatherService CreateService(string jsonResponse)
    {
        var handler = new MockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(handler);
        return new WeatherService(httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetWeatherDataAsync_ValidResponse_ReturnsWeatherDataset()
    {
        // Arrange
        string jsonResponse = @"{
            ""location"": {
                ""name"": ""New York""
            },
            ""current"": {
                ""temperature"": 22,
                ""humidity"": 65,
                ""weather_descriptions"": [""Partly cloudy""]
            }
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<WeatherDataset> result = await service.GetWeatherDataAsync("New York");

        // Assert
        Assert.Single(result);
        Assert.Equal("New York", result[0].Location);
        Assert.Equal(22, result[0].Temperature);
        Assert.Equal(65, result[0].Humidity);
        Assert.Equal("Partly cloudy", result[0].WeatherDescription);
        Assert.NotNull(result[0].Id);
        Assert.NotNull(result[0].OccurredAt);
    }

    [Fact]
    public async Task GetWeatherDataAsync_ZeroTemperature_HandlesCorrectly()
    {
        // Arrange
        string jsonResponse = @"{
            ""location"": {
                ""name"": ""Moscow""
            },
            ""current"": {
                ""temperature"": 0,
                ""humidity"": 80,
                ""weather_descriptions"": [""Freezing""]
            }
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<WeatherDataset> result = await service.GetWeatherDataAsync("Moscow");

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].Temperature);
    }

    [Fact]
    public async Task GetWeatherDataAsync_NegativeTemperature_HandlesCorrectly()
    {
        // Arrange
        string jsonResponse = @"{
            ""location"": {
                ""name"": ""Antarctica""
            },
            ""current"": {
                ""temperature"": -30,
                ""humidity"": 50,
                ""weather_descriptions"": [""Snow""]
            }
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<WeatherDataset> result = await service.GetWeatherDataAsync("Antarctica");

        // Assert
        Assert.Single(result);
        Assert.Equal(-30, result[0].Temperature);
    }

    [Fact]
    public async Task GetWeatherDataAsync_MissingLocationName_ReturnsEmptyString()
    {
        // Arrange
        string jsonResponse = @"{
            ""location"": {
                ""name"": null
            },
            ""current"": {
                ""temperature"": 20,
                ""humidity"": 50,
                ""weather_descriptions"": [""Clear""]
            }
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<WeatherDataset> result = await service.GetWeatherDataAsync("Unknown");

        // Assert
        Assert.Single(result);
        Assert.Equal("", result[0].Location);
    }

    [Fact]
    public async Task GetWeatherDataAsync_InvalidJson_ThrowsException()
    {
        // Arrange
        string invalidJson = "{ invalid json }";
        var service = CreateService(invalidJson);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.GetWeatherDataAsync("Test"));
    }

    [Fact]
    public async Task GetWeatherDataAsync_GeneratesUniqueIds()
    {
        // Arrange
        string jsonResponse = @"{
            ""location"": {
                ""name"": ""London""
            },
            ""current"": {
                ""temperature"": 15,
                ""humidity"": 70,
                ""weather_descriptions"": [""Cloudy""]
            }
        }";

        var service1 = CreateService(jsonResponse);
        var service2 = CreateService(jsonResponse);

        // Act
        List<WeatherDataset> result1 = await service1.GetWeatherDataAsync("London");
        List<WeatherDataset> result2 = await service2.GetWeatherDataAsync("London");

        // Assert
        Assert.NotEqual(result1[0].Id, result2[0].Id);
    }

    [Fact]
    public async Task GetWeatherDataAsync_OccurredAtIsValidIsoFormat()
    {
        // Arrange
        string jsonResponse = @"{
            ""location"": {
                ""name"": ""Paris""
            },
            ""current"": {
                ""temperature"": 18,
                ""humidity"": 55,
                ""weather_descriptions"": [""Sunny""]
            }
        }";

        var service = CreateService(jsonResponse);

        // Act
        List<WeatherDataset> result = await service.GetWeatherDataAsync("Paris");

        // Assert
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z", result[0].OccurredAt);
    }
}
