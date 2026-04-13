using System.Reflection;
using System.Runtime.Loader;

namespace Eva.Drivers.Core.Assembly;

/// <summary>
/// An isolated AssemblyLoadContext for a single driver assembly.
/// Allows unloading and prevents dependency conflicts between drivers.
/// </summary>
internal sealed class DriverAssemblyLoadContext : AssemblyLoadContext

{
    private readonly AssemblyDependencyResolver _resolver;
 
    public DriverAssemblyLoadContext(string assemblyPath)
        : base(name: Path.GetFileNameWithoutExtension(assemblyPath), isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(assemblyPath);
    }
 
    protected override System.Reflection.Assembly? Load(AssemblyName assemblyName)
    {
        // Let Eva.Drivers.Abstractions be resolved from the host to share interfaces
        if (assemblyName.Name == "Eva.Drivers.Abstractions")
            return null;
 
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }
 
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
