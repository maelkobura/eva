using Eva.Commons.Util;
using Eva.Node.Loader;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Service;

public class ServiceLoader
{
    public static ServiceLoader? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<ServiceLoader>();
    private EvaService? service;
    
    public static void Init()
    {
        if (Instance != null) return;
        Instance = new ServiceLoader();
    }

    public EvaService LoadService()
    {
        if (service is not null) return service;
        Type type = AssemblyLoader.Instance!.GetMainType();
        if(type is null) throw new Exception("Unable to get service from classpath");
        if (!type.GetInterfaces().Contains(typeof(EvaService))) throw new Exception("Main type is not a subclass of EvaService");
        
        
        service = (EvaService)Activator.CreateInstance(type);
        
        if(service is null) throw new Exception("Unable to load service");
        
        //TODO Autowired params fill
        service.Initialize();
        
        return service;
    }
}