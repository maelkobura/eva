using System;
using System.Linq;
using System.Reflection;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Configuration;
using Eva.Node.Loader;
using Eva.Node.Service.Functions;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Service;

public class InternalServiceLoader : IServiceLoader
{
    private static ILogger logger = EvaLogger.CreateLogger<InternalServiceLoader>();
    private EvaService? service;
    
    public ServiceDescription? Description { get; private set; }

    public InternalServiceLoader(ServiceDescription description)
    {
        Description = description;
    }

    public EvaService LoadService(string baseConfigPath)
    {
        if (service is not null) return service;
        Type type = EvaSystem.Singleton<IAssemblyLoader>().GetMainType();
        if(type is null) throw new Exception("Unable to get service from classpath");
        if (!type.GetInterfaces().Contains(typeof(EvaService))) throw new Exception("Main type is not a subclass of EvaService");
        
        
        service = (EvaService)Activator.CreateInstance(type);
        
        if(service is null) throw new Exception("Unable to load service");
        
        var methods = service.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<EvaFunctionAttribute>() is not null);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<EvaFunctionAttribute>()!;
            EvaSystem.Singleton<IFunctionRegistry>().RegisterObjectMethod(service, method, attr);
        }
        
        // Inject [Configurationnable] fields
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<ConfigurationableAttribute>() is not null);

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ConfigurationableAttribute>()!;
            
            // Vérification que le field est bien un Configuration<T>
            if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(Configuration<>))
            {
                logger.LogWarning("Field '{Field}' is annotated with [Configurationnable] but is not of type Configuration<T>, skipping.", field.Name);
                continue;
            }

            var configType = field.FieldType.GetGenericArguments()[0];
            var filePath = Path.Combine(baseConfigPath, attr.Name);
            
            // Instanciation de Configuration<T> avec (string filePath, bool hotReload)
            var configInstance = Activator.CreateInstance(
                typeof(Configuration<>).MakeGenericType(configType),
                filePath,
                attr.HotReload
            );

            field.SetValue(service, configInstance);
            logger.LogInformation("Injected config '{Path}' into field '{Field}' (hotReload={HotReload}).", filePath, field.Name, attr.HotReload);
        }
        
        //TODO Autowired params fill
        service.Initialize();
        
        return service;
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}