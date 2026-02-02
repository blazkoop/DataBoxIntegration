using DataboxIntegration.Services;
using DotNetEnv;

Env.Load();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IFileLogger, FileLogger>();
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddHttpClient<MarketService>();
builder.Services.AddHttpClient<DataboxService>();

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();