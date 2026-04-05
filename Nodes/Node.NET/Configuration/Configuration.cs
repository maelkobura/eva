using System.Text.Json;
using System.Xml.Linq;
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
            _logger.LogWarning("Config file '{Path}' not found, creating with default values.", _filePath);
            Value = new T();
            Save();
            return;
        }

        var previous = Value;
        try
        {
            var json = File.ReadAllText(_filePath);
            Value = JsonSerializer.Deserialize<T>(json) ?? previous;
            _logger.LogInformation("Config file '{Path}' loaded successfully.", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read config file '{Path}', keeping previous value.", _filePath);
            Value = previous; // restauration
        }
    }
    
    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        
            var json = JsonSerializer.Serialize(Value, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            _logger.LogInformation("Service configuration file '{Path}' created with default values.", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service configuration file '{Path}'.", _filePath);
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