using Microsoft.Extensions.Logging;

namespace Eva.Commons.Util;

public class EvaLogger
{
    
    public static ILoggerFactory Factory { get; private set; }

    public static void Init()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
    }
    
    public static ILogger CreateLogger<T>()
    {
        return Factory.CreateLogger<T>();
    }
    
}