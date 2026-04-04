using System.Text;
using Eva.Commons.Messages;
using Jint;
using Jint.Native;

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