using System.Reflection;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Loader;
using Eva.Node.Service.Functions;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Service;

public class ServiceLoader
{
    public static ServiceLoader? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<ServiceLoader>();
    private EvaService? service;
    
    public ServiceDescription? Description { get; private set; }
    
    public static void Init(ServiceDescription description)
    {
        if (Instance != null) return;
        Instance = new ServiceLoader(description);
    }

    public ServiceLoader(ServiceDescription description)
    {
        Description = description;
    }

    public EvaService LoadService()
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
        
        
        //TODO Autowired params fill
        service.Initialize();
        
        return service;
    }
}