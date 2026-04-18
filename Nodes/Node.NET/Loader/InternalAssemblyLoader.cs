using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Eva.Commons.System;
using Eva.Node.Service;
using Eva.Node.Types;

namespace Eva.Node.Loader;

public class InternalAssemblyLoader : IAssemblyLoader
{
    private readonly Assembly _assembly;
    private readonly ServiceLoadContext _loadContext;
    private ServiceDescription? _serviceDescription;

    public string NodeAssemblyPath { get; }

    public InternalAssemblyLoader(string nodeAssemblyPath)
    {
        NodeAssemblyPath = nodeAssemblyPath;
        _loadContext = new ServiceLoadContext(nodeAssemblyPath);
        _assembly = _loadContext.LoadFromAssemblyPath(nodeAssemblyPath);
        
        EvaSystem.Singleton<ITypeRegistration>().RegisterAssembly(_assembly);
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
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
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
        return _assembly.GetType(_serviceDescription.Class)
               ?? throw new Exception($"Class '{_serviceDescription.Class}' not found in assembly");
    }

    public void Dispose()
    {
        _loadContext.Unload();
    }

    private sealed class ServiceLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ServiceLoadContext(string assemblyPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(assemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Si l'assembly est déjà chargée dans le contexte par défaut (Eva.Commons, etc.), on la réutilise
            var existing = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            if (existing != null) return existing;

            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path != null ? LoadFromAssemblyPath(path) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
        }
    }
}