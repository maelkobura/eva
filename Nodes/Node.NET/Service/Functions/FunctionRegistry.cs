using System.Reflection;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Service.Functions;

public class FunctionRegistry
{
    public static FunctionRegistry? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<FunctionRegistry>();
    
    private readonly Dictionary<string, FunctionDescriptor> _functions = new();

    public static void Init()
    {
        if (Instance != null) return;
        Instance = new FunctionRegistry();
    }
    
    public void RegisterObjectMethod(object target, MethodInfo method, EvaFunctionAttribute attr)
    {
        var parameters = method.GetParameters()
            .Select(p => new ParameterDescriptor
            {
                Name = p.Name!,
                Type = p.ParameterType,
                IsRequired = !p.HasDefaultValue
            })
            .ToArray();
        
        _functions[method.Name.ToSnakeCase()] = new FunctionDescriptor
        {
            Name = method.Name.ToSnakeCase(),
            Description = attr.Description,
            Keywords = attr.Keywords,
            Authorization = attr.Authorization,
            Parameters = parameters,
            ReturnType = UnwrapReturnType(method.ReturnType),
            Invoke = args => InvokeMethod(target, method, args)
        };
    }

    public void RegisterLambda(
        string name,
        string description,
        string[] keywords,
        Delegate lambda,
        string[] authorization = null)
    {
        var method = lambda.Method;
        var parameters = method.GetParameters()
            .Select(p => new ParameterDescriptor
            {
                Name = p.Name!,
                Type = p.ParameterType,
                IsRequired = !p.HasDefaultValue
            })
            .ToArray();

        _functions[name] = new FunctionDescriptor
        {
            Name = name,
            Description = description,
            Keywords = keywords,
            Authorization = authorization ?? Array.Empty<string>(),
            Parameters = parameters,
            ReturnType = UnwrapReturnType(method.ReturnType),
            Invoke = args => InvokeMethod(lambda.Target, method, args)
        };
    }
    
    public FunctionDescriptor? Get(string name) =>
        _functions.TryGetValue(name, out var f) ? f : null;

    public IEnumerable<FunctionDescriptor> GetAll() => _functions.Values;

    // Helpers
    private static async Task<object?> InvokeMethod(object? target, MethodInfo method, object?[] args)
    {
        var result = method.Invoke(target, args);
        if (result is Task task)
        {
            await task;
            
            var resultProp = task.GetType().GetProperty("Result");
            return resultProp?.GetValue(task);
        }
        return result;
    }

    private static Type UnwrapReturnType(Type type)
    {
        
        if (type == typeof(Task)) return typeof(void);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            return type.GetGenericArguments()[0];
        return type;
    }
}
    
