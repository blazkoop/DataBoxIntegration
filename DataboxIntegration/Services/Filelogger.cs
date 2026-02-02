namespace DataboxIntegration.Services;

public interface IFileLogger
{
    void LogDataSend(string serviceProvider, int rows, int columns, bool success, string? errorMessage = null);
}

public class FileLogger : IFileLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new object();

    public FileLogger()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        _logFilePath = Path.Combine(currentDirectory, "integration.log");
    }

    public void LogDataSend(string serviceProvider, int rows, int columns, bool success, string? errorMessage = null)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        string status = success ? "SUCCESS" : "FAILURE";
        
        string logEntry = $"[{timestamp}] Provider: {serviceProvider} | Status: {status} | Rows: {rows} | Columns: {columns}";
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            logEntry += $" | Error: {errorMessage}";
        }

        lock (_lock)
        {
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }
        
        Console.WriteLine(logEntry);
    }
}