using System.Text.Json;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Configuration;

public class Configuration<T> where T : new()
{
    public T Value { get; private set; } = new();

    private readonly string _filePath;
    private FileSystemWatcher? _watcher;
    private static readonly ILogger _logger = EvaLogger.CreateLogger<Configuration<T>>();

    public Configuration(string filePath, bool hotReload = true)
    {
        _filePath = filePath;
        Load();
        if (hotReload)
            StartWatcher();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("Service configuration file '{Path}' not found, using default values.", _filePath);
            Value = new T();
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            Value = JsonSerializer.Deserialize<T>(json) ?? new T();
            _logger.LogInformation("Service configuration file '{Path}' loaded successfully.", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read config file '{Path}'.", _filePath);
            Value = new T();
        }
    }

    private void StartWatcher()
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_filePath)) ?? ".";
        var file = Path.GetFileName(_filePath);

        _watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += (_, _) =>
        {
            Thread.Sleep(100);
            _logger.LogInformation("Change detected in '{Path}', reloading...", _filePath);
            Load();
        };
    }
}