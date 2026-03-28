namespace Eva.Node.Service.Functions;

[AttributeUsage(AttributeTargets.Method)]
public class EvaFunctionAttribute : Attribute
{
    public string Description { get; set; } = "";
    public string[] Keywords { get; set; } = [];
    public string[] Authorization { get; set; } = [];
}