using System.Text.RegularExpressions;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.Commons.System;

public class EvaSystem
{
    private static readonly ILogger logger = EvaLogger.CreateLogger<EvaSystem>();
    
    
    private static readonly Dictionary<Type, IDisposable> _singletons = new();

    public static TSingleton AddSingleton<TInterface, TSingleton>(params object[]? args) where TSingleton : class where TInterface : IDisposable{
        Type inter = typeof(TInterface);
        Type typeSingleton = typeof(TSingleton);
        
        if(!typeof(TSingleton).IsAssignableTo(inter)) throw new ArgumentException("The type " + inter.Name + " must be an interface of " + typeSingleton.Name);
        if(_singletons.ContainsKey(inter)) throw new ArgumentException("The singleton " + inter.Name + " is already registered");
        
        logger.LogInformation("Initializating " + typeSingleton.Name.SplitSingletonName() +"...");
        
        var instance = (TSingleton)Activator.CreateInstance(typeof(TSingleton), args)!;
        _singletons[typeof(TInterface)] = (IDisposable)instance;
        return instance;
    }

#if DEBUG
    public static TInterface AddSingleton<TInterface>(TInterface instance) where TInterface : class, IDisposable
    {
        Type inter = typeof(TInterface);
        if (_singletons.ContainsKey(inter)) throw new ArgumentException("The singleton " + inter.Name + " is already registered");
        _singletons[inter] = (IDisposable)instance;
        return instance;
    }
#endif
    
    public static TInterface Singleton<TInterface>() where TInterface : class {
        if(!_singletons.ContainsKey(typeof(TInterface))) throw new ArgumentException("The singleton " + typeof(TInterface).Name + " is not registered");
        return (TInterface)_singletons[typeof(TInterface)];
    }

    public static void Clear()
    {
        logger.LogWarning("Clearing singletons...");
        foreach (var singleton in _singletons.Values)
        {
            singleton.Dispose();
        }
        _singletons.Clear();
    }
}