namespace Eva.Drivers.Abstractions.Drivers;

public interface EvaDriver
{
    public string Name { get; set; }
    public Dictionary<string, string> Configuration { get; set; }
    
    public void Initialize();
    public void Shutdown();

}