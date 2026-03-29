using Eva.Commons.Messages;

namespace Eva.Commons.Util;

public class FunctionsUtil
{
    public static ReturnType MapType(Type type)
    {
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            return new ReturnType { Type = EvaType.Array, ArrayType = MapPrimitive(elementType) };
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GetGenericArguments()[0];
            return new ReturnType { Type = EvaType.Array, ArrayType = MapPrimitive(elementType) };
        }

        return new ReturnType { Type = MapPrimitive(type) };
    }

    private static EvaType MapPrimitive(Type type) => type switch
    {
        _ when type == typeof(string)   => EvaType.String,
        _ when type == typeof(int)      => EvaType.Int32,
        _ when type == typeof(long)     => EvaType.Int64,
        _ when type == typeof(bool)     => EvaType.Boolean,
        _ when type == typeof(float)    => EvaType.Float,
        _ when type == typeof(double)   => EvaType.Double,
        _ when type == typeof(DateTime) => EvaType.Timestamp,
        _ when type == typeof(byte[])   => EvaType.Bytes,
        _                               => EvaType.Object
    };
}