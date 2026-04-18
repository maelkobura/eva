using System.Text;
using Eva.Commons.Messages;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
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

                args[i] = DeserializeParameter(bytes.ToByteArray(), param.Type, FunctionsUtil.MapPrimitive(param.Type));
            }

            // 3. Invoke the function
            var result = await _descriptor.Invoke(args);

            // 4. Serialize result to bytes
            var serialized = SerializeResult(result, _descriptor.ReturnType, request.InJson);
            return new InvokeResponse { Success = true, Result = serialized };
        }
        catch (Exception ex)
        {
            return Error("INTERNAL_ERROR", ex.Message);
        }
    }

    private object? DeserializeParameter(byte[] bytes, Type paramType, EvaType evaType)
    {
        if (evaType != EvaType.Object)
        {
            return evaType switch
            {
                EvaType.String    => Encoding.UTF8.GetString(bytes),
                EvaType.Int32     => BitConverter.ToInt32(bytes),
                EvaType.Int64     => BitConverter.ToInt64(bytes),
                EvaType.Boolean   => BitConverter.ToBoolean(bytes),
                EvaType.Float     => BitConverter.ToSingle(bytes),
                EvaType.Double    => BitConverter.ToDouble(bytes),
                EvaType.Timestamp => DateTime.FromBinary(BitConverter.ToInt64(bytes)),
                EvaType.Bytes     => bytes,
                _                 => bytes
            };
        }

        // EvaObject path
        if (!typeof(IMessage).IsAssignableFrom(paramType))
            return bytes; // type inconnu, on retourne les bytes bruts

        var package = EvaObjectPackage.Parser.ParseFrom(bytes);
        var descriptor = EvaSystem.Singleton<ITypeRegistration>().Registry.Find(package.TypeUrl);
        if (descriptor is null)
            throw new InvalidOperationException($"Type non trouvé dans le TypeRegistry: '{package.TypeUrl}'");

        var parser = descriptor.Parser;
        return parser.ParseFrom(package.Payload);
    }

    private static ByteString SerializeResult(object? result, Type type, bool inJson)
    {
        if (result is null) return ByteString.Empty;

        if (result is IMessage message)
        {
            var package = new EvaObjectPackage
            {
                TypeUrl = message.Descriptor.FullName,
                Payload = inJson
                    ? ByteString.CopyFromUtf8(JsonFormatter.Default.Format(message))
                    : message.ToByteString()
            };
            return package.ToByteString();
        }

        var bytes = result switch
        {
            string s    => Encoding.UTF8.GetBytes(s),
            int i       => BitConverter.GetBytes(i),
            long l      => BitConverter.GetBytes(l),
            bool b      => BitConverter.GetBytes(b),
            float f     => BitConverter.GetBytes(f),
            double d    => BitConverter.GetBytes(d),
            DateTime dt => BitConverter.GetBytes(dt.ToBinary()),
            byte[] raw  => raw,
            _           => []
        };

        return ByteString.CopyFrom(bytes);
    }

    private static InvokeResponse Error(string code, string message) => new()
    {
        Success = false,
        Error = $"[{code}] {message}"
    };
}