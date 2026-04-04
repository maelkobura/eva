using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Eva.Node.Service;

namespace Eva.Node.Loader;

public class InternalAssemblyLoader : IAssemblyLoader
{
    private readonly Assembly _assembly;
    private ServiceDescription? _serviceDescription;

    public string NodeAssemblyPath { get; }

    public InternalAssemblyLoader(string nodeAssemblyPath)
    {
        NodeAssemblyPath = nodeAssemblyPath;
        _assembly = Assembly.LoadFrom(nodeAssemblyPath);
    }

    public ServiceDescription LoadDescription()
    {
        if (_serviceDescription != null) return _serviceDescription;

        var metadata = _assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(a => a.Key, a => a.Value);

        string[]? authorization = null;
        if (metadata.TryGetValue("Authorization", out var authResourceName))
        {
            using var stream = _assembly.GetManifestResourceStream(authResourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                authorization = reader
                    .ReadToEnd()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        _serviceDescription = new ServiceDescription(
            Name: metadata.GetValueOrDefault("Name") ?? "",
            Authorization: authorization,
            Class: metadata.GetValueOrDefault("Class") ?? "",
            DisplayName: metadata.GetValueOrDefault("DisplayName") ?? "Default display name",
            Description: metadata.GetValueOrDefault("Description") ?? "Default description",
            Version: metadata.GetValueOrDefault("Version") ?? "1.0.0",
            Author: metadata.GetValueOrDefault("Author") ?? "No author",
            License: metadata.GetValueOrDefault("License") ?? "No license"
        );

        return _serviceDescription;
    }

    public Type GetMainType()
    {
        if (_serviceDescription == null) throw new Exception("Service description not loaded");
        return _assembly.GetType(_serviceDescription.Class);
    }

    public void Dispose()
    {}
}