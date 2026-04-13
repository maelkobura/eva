using Eva.Commons.Util;
using Eva.Drivers.Abstractions.Drivers;
using Eva.Drivers.Core.Assembly;
using Eva.Drivers.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Eva.Drivers.Core;

public class DriverLoader : IDisposable {
    
    private static ILogger logger = EvaLogger.CreateLogger<DriverLoader>();
    
    public Dictionary<string, EvaDriver> Drivers = new();
    public DriverConfiguration Configuration { get; set; }
    public string DriverPath { get; set; }
    public Type[] DriverTypes { get; set; } = Array.Empty<Type>();
    
    public DriverLoader(DriverConfiguration configuration) {
        Configuration = configuration;
    }

    private AssemblyLoader _assemblyLoader = new();
    
    public void Load() {
        if(Drivers.Count != 0) return;
        logger.LogInformation("Loading drivers from {Path}...", DriverPath);
        _Load();
    }

    public void Reload()
    {
        logger.LogInformation("Reloading drivers from {Path}...", DriverPath);
        Drivers.Clear();
        _Load();
    }
    
    private void _Load() {
        logger.LogInformation("Loading assemblies from {DriverPath}", DriverPath);
        _assemblyLoader.Load(DriverPath);
        foreach (var model in Configuration.Models)
        {
            logger.LogInformation("Loading model {Model}...", model.Name);
            if (!_assemblyLoader.LoadedTypes.TryGetValue(model.Class, out var driverType))
            {
                logger.LogWarning("Could not find driver type {DriverType}.", model.Class);
                continue;
            }
            
            var driver = (EvaDriver)Activator.CreateInstance(driverType);
            driver.Name = model.Name;
            driver.Configuration = model.Configuration;
            driver.Initialize();
            Drivers.Add(model.Name, driver);
        }
        logger.LogInformation("Loaded {Count} drivers", Drivers.Count);
    }

    public T GetModel<T>(string name) where T : EvaDriver
    {
        return  (T)Drivers[name];
    }
        
    public void Dispose()
    {
        _assemblyLoader.Dispose();
        Drivers.Clear();
    }
}