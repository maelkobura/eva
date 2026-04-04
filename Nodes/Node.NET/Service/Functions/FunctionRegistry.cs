using System.Reflection;
using Eva.Commons.Util;
using Eva.Commons.Messages;
using Google.Protobuf.Collections;
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
            Depreciated = attr.Depreciated,
            Weight = attr.Weight,
            Flags = attr.Flags,
            Invoke = args => InvokeMethod(target, method, args)
        };
    }

    public void RegisterLambda(
        string name,
        string description,
        string[] keywords,
        Delegate lambda,
        bool depreciated = false,
        int weight = 0,
        string[] flags = null,
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
            Invoke = args => InvokeMethod(lambda.Target, method, args),
            Depreciated = depreciated,
            Weight = weight,
            Flags = flags ?? Array.Empty<string>()
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
    
    public EvaFunctionDescriptor GetDescriptor(FunctionDescriptor descriptor)
    {
        var evadesc = new EvaFunctionDescriptor()
        {
            Id = descriptor.Id,
            Name = descriptor.Name,
            Description = descriptor.Description,
            ReturnType = FunctionsUtil.MapType(descriptor.ReturnType),
            Depreciated = descriptor.Depreciated,
            Weight = descriptor.Weight
        };
        
        
        evadesc.Flags.Add(descriptor.Flags);
        evadesc.Keywords.Add(descriptor.Keywords);
        evadesc.Authorization.Add(descriptor.Authorization);

        foreach (var param in descriptor.Parameters)
        {
            evadesc.Parameters.Add(new EvaParameterDescriptor
            {
                Name = param.Name,
                Type = FunctionsUtil.MapType(param.Type),
                IsRequired = param.IsRequired
            });
        }

        return evadesc;
    }

    public FunctionPanel GetPanel()
    {
        var panel = new FunctionPanel
        {
            ServiceId = ServiceLoader.Instance!.Description!.Name,
        };

        panel.Functions.Add(_functions.Values.Select(GetDescriptor));

        return panel;
    }
    
    public FunctionExecutor? CreateExecutor(string functionName, bool skipAuthorization = false)
    {
        var descriptor = Get(functionName);
        if (descriptor is null) return null;
        return new FunctionExecutor(descriptor, skipAuthorization);
    }
    
}
    
