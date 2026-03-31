namespace Eva.Node.Service.Functions;

[AttributeUsage(AttributeTargets.Method)]
public class EvaFunctionAttribute : Attribute
{
    public string Description { get; set; } = "";
    public string[] Keywords { get; set; } = [];
    public string[] Authorization { get; set; } = [];
    public bool Depreciated { get; set; } = false;
    public int Weight { get; set; } = 0;
    public string[] Flags { get; set; } = [];
}