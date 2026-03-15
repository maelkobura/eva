using System.Reflection;
using Eva.Node.Service;

namespace Eva.Node.Loader;

public class AssemblyLoader
{
    public static AssemblyLoader? Instance { get; private set; }
    
    private Assembly? assembly;
    public string NodeAssemblyPath { get; }

    public static void Init(string nodeAssemblyPath)
    {
        if (Instance != null) return;
        Instance = new AssemblyLoader(nodeAssemblyPath);
    }
    
    public AssemblyLoader(string nodeAssemblyPath)
    {
        NodeAssemblyPath = nodeAssemblyPath;
        assembly = Assembly.LoadFrom(nodeAssemblyPath);
    }

    public ServiceDescription LoadDescription()
    {
        if(assembly == null) throw new Exception("Assembly not loaded");
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(a => a.Key, a => a.Value);

        string[]? authorization = null;
        if (metadata.TryGetValue("Authorization", out var authResourceName))
        {
            using var stream = assembly.GetManifestResourceStream(authResourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                authorization = reader
                    .ReadToEnd()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        return new ServiceDescription(
            Name: metadata.GetValueOrDefault("Name") ?? "",
            Authorization: authorization,
            Class: metadata.GetValueOrDefault("Class") ?? "",
            DisplayName: metadata.GetValueOrDefault("DisplayName") ?? "Default display name",
            Description: metadata.GetValueOrDefault("Description") ?? "Default description",
            Version: metadata.GetValueOrDefault("Version") ?? "1.0.0",
            Author: metadata.GetValueOrDefault("Author") ?? "No author",
            License: metadata.GetValueOrDefault("License") ?? "No license"
        );
    }
    
}