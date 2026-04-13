using Eva.Commons.Util;
using Eva.Drivers.Abstractions.Drivers;
using Microsoft.Extensions.Logging;

namespace Eva.Drivers.Core.Assembly;

internal class AssemblyLoader : IDisposable {
    
    private static ILogger logger = EvaLogger.CreateLogger<AssemblyLoader>();
    
    public Dictionary<string, Type> LoadedTypes { get; set; } = new();
    public List<DriverAssemblyLoadContext> _assemblyLoadContexts = new();
    
    public void Load(string path) {
        Dispose();
        
        var dllPaths = Directory.GetFiles(path, "*.driver.dll", SearchOption.TopDirectoryOnly);

        foreach (var dllPath in dllPaths)
        {
            try
            {
                var context = new DriverAssemblyLoadContext(dllPath);
                _assemblyLoadContexts.Add(context);
                var assembly = context.LoadFromAssemblyPath(dllPath);
                var driverTypes = assembly.GetTypes()
                    .Where(t => t is { IsAbstract: false, IsInterface: false }
                                && typeof(EvaDriver).IsAssignableFrom(t)
                                && t.GetConstructor(Type.EmptyTypes) != null)
                    .ToList();
                
                foreach (var driverType in driverTypes)
                {
                    var name = driverType.FullName;
                    if (name is null)
                    {
                        logger.LogWarning(driverType.Name + " isn't valid.");
                        continue;
                    }
                    if (!LoadedTypes.TryAdd(name, driverType))
                    {
                        logger.LogWarning("Driver type {Name} already loaded", name);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to load assembly {dllPath}", dllPath);
            }
        }
        
        logger.LogInformation("Loaded {count} driver types", LoadedTypes.Count);
        
    }
    
    
    public void Dispose()
    {
        LoadedTypes.Clear();
        foreach (var context in _assemblyLoadContexts)
        {
            context.Unload();
        }
        _assemblyLoadContexts.Clear();
    }
}