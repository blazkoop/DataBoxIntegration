# Databox Integration API

A .NET 8 Web API that fetches data from external providers (Weatherstack, Marketstack) and sends it to Databox.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- API keys for:
  - [Weatherstack](https://weatherstack.com/) - weather data
  - [Marketstack](https://marketstack.com/) - stock market data
  - [Databox](https://databox.com/) - data visualization

## Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/blazkoop/DataBoxIntegration.git
   cd DataBoxIntegration
   ```

2. Create a `.env` file in the `DataboxIntegration` folder:
   ```bash
   cd DataboxIntegration
   cp .env.example .env
   ```

3. Edit `.env` and add your API keys and Databox configuration:
   ```
   ApiKeys__Weatherstack=your_weatherstack_api_key
   ApiKeys__Marketstack=your_marketstack_api_key
   ApiKeys__Databox=your_databox_api_key
   Databox__WeatherDatasetId=your_weather_dataset_id
   Databox__MarketDatasetId=your_market_dataset_id
   ```

4. Restore dependencies:
   ```bash
   dotnet restore
   ```

## Running the Application

```bash
cd DataboxIntegration
dotnet run
```

The API will be available at `http://localhost:5240`

Swagger UI: `http://localhost:5240/swagger`

## API Endpoints

### Weather

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sendWeatherData/{location}` | Fetch weather data and send to Databox |
| GET | `/api/weather/preview/{location}` | Preview weather data without sending |

**Example:**
```bash
curl -X POST http://localhost:5240/api/sendWeatherData/London
```

### Market

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sendMarketData/{symbols}?limit=5` | Fetch market data and send to Databox |
| GET | `/api/market/preview/{symbols}?limit=5` | Preview market data without sending |

**Example:**
```bash
curl -X POST "http://localhost:5240/api/sendMarketData/AAPL,MSFT?limit=10"
```

## Running Tests

```bash
dotnet test
```

## Project Structure

```
DataboxIntegration/
├── Controllers/        # API endpoints
├── Models/            # Data models
├── Services/          # Business logic
├── Program.cs         # Application entry point
└── appsettings.json   # Configuration
```
