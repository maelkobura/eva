using System.Text;
using Eva.Commons.Messages;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Node.Service;
using Eva.Node.Service.Calling;
using Eva.Node.Service.Functions;
using Google.Protobuf;

namespace Eva.Node.Network;

/// <summary>
/// Utility class for communication between services
/// </summary>
public static class EvaServices
{
    /// <summary>
    /// Call service function with explicit borrow certificate
    /// </summary>
    /// <param name="fullName">The full name of the function to call (e.g. "service.function_name").</param>
    /// <param name="cert">The borrow certificate to use for authentication.</param>
    /// <param name="parameters">The parameters to pass to the function.</param>
    /// <returns>The result of the function call.</returns>
    public static async Task<T> Call<T>(string fullName, Certificate cert, params object?[] parameters)
    {
        return await EvaSystem.Singleton<IServiceRouter>().Call<T>(fullName, cert, parameters);
    }

    /// <summary>
    /// Call service function with node certificate
    /// </summary>
    /// <param name="fullName">The full name of the function to call (e.g. "service.function_name").</param>
    /// <param name="parameters">The parameters to pass to the function.</param>
    /// <returns>The result of the function call.</returns>
    public static Task<T> Call<T>(string fullName, params object?[] parameters)
    {
        return EvaSystem.Singleton<IServiceRouter>().Call<T>(fullName, parameters);
    }
    

    /// <summary>
    /// Check if a service is available
    /// </summary>
    /// <param name="name">The name of the service to check.</param>
    /// <returns>True if the service is available, false otherwise.</returns>
    public static bool IsServiceAvailable(string name)
    {
        return !EvaSystem.Singleton<INetworkNodeManager>().Nodes.First(entity => entity.Name == name).IsExpirated();
    }
}
