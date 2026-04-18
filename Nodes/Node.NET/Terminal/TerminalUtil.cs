using System.Text;
using Eva.Commons.Messages;
using Jint;
using Jint.Native;
using Jint.Runtime;

namespace Eva.Node.Terminal;

public class TerminalUtil
{
    public static object? ConvertFromJavascript(JsValue value, EvaType type) => type switch
    {
        EvaType.String    => value.AsString(),
        EvaType.Int32     => (int)value.AsNumber(),
        EvaType.Int64     => (long)value.AsNumber(),
        EvaType.Boolean   => value.AsBoolean(),
        EvaType.Float     => (float)value.AsNumber(),
        EvaType.Double    => value.AsNumber(),
        EvaType.Timestamp => DateTime.FromBinary((long)value.AsNumber()),
        _                 => value.ToString()
    };

public static (ReturnType type, byte[] value) ConvertFromJavascript(JsValue value)
{
    if (value.IsArray())
    {
        var array = value.AsArray();
        int length = (int)array.Length;

        if (length == 0)
        {
            return (
                new ReturnType
                {
                    Type = EvaType.Array,
                    ArrayType = EvaType.String // fallback arbitraire
                },
                Array.Empty<byte>()
            );
        }

        // Détection du type du premier élément
        var (firstType, firstBytes) = ConvertFromJavascript(array.Get(0));

        var buffer = new List<byte>();

        for (int i = 0; i < length; i++)
        {
            var (elemType, elemBytes) = ConvertFromJavascript(array.Get(i));

            // Sécurité : homogénéité
            if (elemType.Type != firstType.Type)
                throw new JavaScriptException("Array must be homogeneous");

            // Préfixe longueur (int32)
            buffer.AddRange(BitConverter.GetBytes(elemBytes.Length));
            buffer.AddRange(elemBytes);
        }

        return (
            new ReturnType
            {
                Type = EvaType.Array,
                ArrayType = firstType.Type
            },
            buffer.ToArray()
        );
    }

    // Cas non-array (ta version précédente)
    return value.Type switch
    {
        Jint.Runtime.Types.String => (
            new ReturnType { Type = EvaType.String },
            Encoding.UTF8.GetBytes(value.AsString())
        ),

        Jint.Runtime.Types.Number => (
            new ReturnType { Type = EvaType.Double },
            BitConverter.GetBytes(value.AsNumber())
        ),

        Jint.Runtime.Types.Boolean => (
            new ReturnType { Type = EvaType.Boolean },
            BitConverter.GetBytes(value.AsBoolean())
        ),
        

        Jint.Runtime.Types.Null => (
            new ReturnType { Type = EvaType.String },
            Array.Empty<byte>()
        ),

        Jint.Runtime.Types.Undefined => (
            new ReturnType { Type = EvaType.String },
            Array.Empty<byte>()
        ),

        _ => (
            new ReturnType { Type = EvaType.String },
            Encoding.UTF8.GetBytes(value.ToString())
        )
    };
}
    

    public static JsValue ConvertToJavascript(byte[] bytes, EvaType type) => type switch
    {
        EvaType.String    => new JsString(Encoding.UTF8.GetString(bytes)),
        EvaType.Int32     => new JsNumber(BitConverter.ToInt32(bytes)),
        EvaType.Int64     => new JsNumber(BitConverter.ToInt64(bytes)),
        EvaType.Boolean   => BitConverter.ToBoolean(bytes) ? JsBoolean.True : JsBoolean.False,
        EvaType.Float     => new JsNumber(BitConverter.ToSingle(bytes)),
        EvaType.Double    => new JsNumber(BitConverter.ToDouble(bytes)),
        EvaType.Timestamp => new JsNumber(BitConverter.ToInt64(bytes)),
        _                 => JsValue.Undefined
    };
}