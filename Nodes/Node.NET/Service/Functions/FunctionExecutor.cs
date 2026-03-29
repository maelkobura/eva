using System.Text;
using Eva.Commons.Messages;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;
using Google.Protobuf;
using Type = System.Type;

namespace Eva.Node.Service.Functions;

public class FunctionExecutor
{
    private readonly FunctionDescriptor _descriptor;
    private readonly bool _skipAuthorization;

    public FunctionExecutor(FunctionDescriptor descriptor, bool skipAuthorization = false)
    {
        _descriptor = descriptor;
        _skipAuthorization = skipAuthorization;
    }

    public async Task<InvokeResponse> ExecuteAsync(InvokeRequest request, Certificate cert)
    {
        try
        {
            // 1. Authorization check
            if (!_skipAuthorization && _descriptor.Authorization.Length > 0)
            {
                if (!_descriptor.Authorization.Any(auth => Authorizations.Has(cert, auth)))
                    return Error("UNAUTHORIZED", $"Caller '{request.CallerId}' is not authorized to invoke '{_descriptor.Name}'");
            }

            // 2. Deserialize parameters from bytes to C# types
            var args = new object?[_descriptor.Parameters.Length];
            for (int i = 0; i < _descriptor.Parameters.Length; i++)
            {
                var param = _descriptor.Parameters[i];
                if (!request.Parameters.TryGetValue(param.Name, out var bytes))
                {
                    if (param.IsRequired)
                        return Error("MISSING_PARAMETER", $"Required parameter missing: '{param.Name}'");
                    args[i] = null;
                    continue;
                }

                args[i] = DeserializeParameter(bytes.ToByteArray(), FunctionsUtil.MapPrimitive(param.Type));
            }

            // 3. Invoke the function
            var result = await _descriptor.Invoke(args);

            // 4. Serialize result to bytes
            var serialized = SerializeResult(result, _descriptor.ReturnType);
            return new InvokeResponse { Success = true, Result = serialized };
        }
        catch (Exception ex)
        {
            return Error("INTERNAL_ERROR", ex.Message);
        }
    }

    private static object? DeserializeParameter(byte[] bytes, EvaType type) => type switch
    {
        EvaType.String    => Encoding.UTF8.GetString(bytes),
        EvaType.Int32     => BitConverter.ToInt32(bytes),
        EvaType.Int64     => BitConverter.ToInt64(bytes),
        EvaType.Boolean   => BitConverter.ToBoolean(bytes),
        EvaType.Float     => BitConverter.ToSingle(bytes),
        EvaType.Double    => BitConverter.ToDouble(bytes),
        EvaType.Timestamp => DateTime.FromBinary(BitConverter.ToInt64(bytes)),
        EvaType.Bytes     => bytes,
        _                 => bytes //TODO EvaObject
    };

    private static ByteString SerializeResult(object? result, Type type)
    {
        if (result is null) return ByteString.Empty;

        var bytes = result switch
        {
            string s   => Encoding.UTF8.GetBytes(s),
            int i      => BitConverter.GetBytes(i),
            long l     => BitConverter.GetBytes(l),
            bool b     => BitConverter.GetBytes(b),
            float f    => BitConverter.GetBytes(f),
            double d   => BitConverter.GetBytes(d),
            DateTime dt => BitConverter.GetBytes(dt.ToBinary()),
            byte[] raw => raw,
            _          => [] //TODO EvaObject
        };

        return ByteString.CopyFrom(bytes);
    }

    private static InvokeResponse Error(string code, string message) => new()
    {
        Success = false,
        Error = $"[{code}] {message}"
    };
}