namespace Eva.Node.Configuration;

[AttributeUsage(AttributeTargets.Field)]
public class ConfigurationableAttribute : Attribute
{
    public string Name { get; }
    public bool HotReload { get; }

    public ConfigurationableAttribute(string name = "config.json", bool hotReload = true)
    {
        Name = name;
        HotReload = hotReload;
    }
}