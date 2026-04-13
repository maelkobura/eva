namespace Eva.Drivers.Core.Configuration;

public class DriverConfiguration
{
    public List<ModelConfiguration> Models { get; set; } = new();
}

public class ModelConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public Dictionary<string, string> Configuration { get; set; } = new();
}