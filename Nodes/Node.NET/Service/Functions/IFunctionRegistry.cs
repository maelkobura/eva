using System;
using System.Collections.Generic;
using System.Reflection;
using Eva.Commons.Messages;

namespace Eva.Node.Service.Functions;

/// <summary>
/// Interface for registering and retrieving node functions.
/// </summary>
public interface IFunctionRegistry : IDisposable
{
    void RegisterObjectMethod(object target, MethodInfo method, EvaFunctionAttribute attr);
    
    void RegisterLambda(
        string name,
        string description,
        string[] keywords,
        Delegate lambda,
        bool depreciated = false,
        int weight = 0,
        string[] flags = null!,
        string[] authorization = null!);

    FunctionDescriptor? Get(string name);
    
    IEnumerable<FunctionDescriptor> GetAll();
    
    EvaFunctionDescriptor GetDescriptor(FunctionDescriptor descriptor);
    
    FunctionPanel GetPanel();
    
    FunctionExecutor? CreateExecutor(string functionName, bool skipAuthorization = false);
}
