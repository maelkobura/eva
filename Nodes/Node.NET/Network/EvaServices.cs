using System.Text;
using Eva.Commons.Messages;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Node.Service;
using Eva.Node.Service.Functions;
using Google.Protobuf;

namespace Eva.Node.Network;

public static class EvaServices
{
    
    // Full call with explicit cert
    public static async Task<T> Call<T>(string fullName, Certificate cert, params object?[] parameters)
    {
        var lastDot = fullName.LastIndexOf('.');
        if (lastDot < 0)
            throw new ArgumentException($"Invalid function name '{fullName}', expected format 'node.function_name'");

        var nodeId = fullName[..lastDot];
        var functionName = fullName[(lastDot + 1)..];

        var node = EvaSystem.Singleton<INetworkNodeManager>().Nodes.FirstOrDefault(n => n.Name == nodeId);
        
        // Loopback
        if (nodeId == ServiceLoader.Instance!.Description!.Name)
        {
            var localDescriptor = FunctionRegistry.Instance.Get(functionName);
            if (localDescriptor is null)
                throw new Exception($"Function '{functionName}' not found locally");

            var executor = new FunctionExecutor(localDescriptor, skipAuthorization: true);
            var loopbackRequest = new InvokeRequest { CallerId = cert.Payload.Content.Subject };
            // sérialise quand même les params pour rester cohérent
            for (int i = 0; i < localDescriptor.Parameters.Length; i++)
            {
                if (i >= parameters.Length) break;
                if (parameters[i] is null) continue;
                loopbackRequest.Parameters[localDescriptor.Parameters[i].Name] =
                    ByteString.CopyFrom(SerializeParameter(parameters[i]!));
            }

            var loopbackResponse = await executor.ExecuteAsync(loopbackRequest, cert);
            if (!loopbackResponse.Success)
                throw new Exception(loopbackResponse.Error);

            return DeserializeResult<T>(loopbackResponse.Result.ToByteArray());
        }

        if (node is null)
            throw new Exception($"Node '{nodeId}' not found");

        var descriptor = node.GetFunction(functionName);
        if (descriptor is null)
            throw new Exception($"Function '{functionName}' not found on node '{nodeId}'");
        
        
        var serialized = new Dictionary<string, ByteString>();
        for (int i = 0; i < descriptor.Parameters.Count; i++)
        {
            if (i >= parameters.Length) break;
            if (parameters[i] is null) continue;
            serialized[descriptor.Parameters[i].Name] = ByteString.CopyFrom(SerializeParameter(parameters[i]!));
        }

        var response = await node.InvokeAsync(functionName, serialized, cert);
        if (!response.Success)
            throw new Exception(response.Error);

        return DeserializeResult<T>(response.Result.ToByteArray());
    }

    // Alias : uses node's own certificate
    public static Task<T> Call<T>(string fullName, params object?[] parameters)
    {
        var lastDot = fullName.LastIndexOf('.');
        var nodeId = fullName[..lastDot];

        var node = EvaSystem.Singleton<INetworkNodeManager>().Nodes.FirstOrDefault(n => n.Name == nodeId);
        if (node is null)
            throw new Exception($"Node '{nodeId}' not found");

        return Call<T>(fullName, node.NodeTrustCertificate!, parameters);
    }

    private static T DeserializeResult<T>(byte[] bytes)
    {
        object result = typeof(T) switch
        {
            _ when typeof(T) == typeof(string)   => Encoding.UTF8.GetString(bytes),
            _ when typeof(T) == typeof(int)      => BitConverter.ToInt32(bytes),
            _ when typeof(T) == typeof(long)     => BitConverter.ToInt64(bytes),
            _ when typeof(T) == typeof(bool)     => BitConverter.ToBoolean(bytes),
            _ when typeof(T) == typeof(float)    => BitConverter.ToSingle(bytes),
            _ when typeof(T) == typeof(double)   => BitConverter.ToDouble(bytes),
            _ when typeof(T) == typeof(DateTime) => DateTime.FromBinary(BitConverter.ToInt64(bytes)),
            _ when typeof(T) == typeof(byte[])   => bytes,
            _ => throw new ArgumentException($"Unsupported return type: {typeof(T).Name}")
        };

        return (T)result;
    }

    private static byte[] SerializeParameter(object value) => value switch
    {
        string s    => Encoding.UTF8.GetBytes(s),
        int i       => BitConverter.GetBytes(i),
        long l      => BitConverter.GetBytes(l),
        bool b      => BitConverter.GetBytes(b),
        float f     => BitConverter.GetBytes(f),
        double d    => BitConverter.GetBytes(d),
        DateTime dt => BitConverter.GetBytes(dt.ToBinary()),
        byte[] raw  => raw,
        _           => throw new ArgumentException($"Unsupported parameter type: {value.GetType().Name}")
    };

    public static bool IsServiceAvailable(string name)
    {
        return !EvaSystem.Singleton<INetworkNodeManager>().Nodes.First(entity => entity.Name == name).IsExpirated();
    }
}
