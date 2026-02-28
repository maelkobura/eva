using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Eva.Commons.Util;

public class EvaLogger
{
    public static ILoggerFactory Factory { get; private set; }

    public static void Init(string appName = "Eva Application")
    {
        Factory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole(options =>
            {
                options.FormatterName = "EvaFormatStyle";
            }).AddConsoleFormatter<EvaFormatConsoleFormatter, ConsoleFormatterOptions>(opts => {});
        });

        EvaFormatConsoleFormatter.AppName = appName;
    }

    public static ILogger CreateLogger<T>()
    {
        return Factory.CreateLogger<T>();
    }
}

// Formatter custom
public class EvaFormatConsoleFormatter : ConsoleFormatter
{
    public static string AppName = "EvaApp"; // Nom de l'app global

    public EvaFormatConsoleFormatter() : base("EvaFormatStyle") { }

    public override void Write<TState>(in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string level = logEntry.LogLevel.ToString().PadRight(5); // Ex: INFO 
        string category = logEntry.Category; // ex: c.e.MyClass
        string msg = logEntry.Formatter(logEntry.State, logEntry.Exception);

        textWriter.WriteLine($"{timestamp}  {level} [{AppName}] {category} - {msg}");
    }
}