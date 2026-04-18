using System.Reflection;
using Eva.Commons.Util;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Types;

public class InternalTypeRegistration : ITypeRegistration
{
    private static readonly ILogger logger = EvaLogger.CreateLogger<InternalTypeRegistration>();

    private readonly HashSet<string> _registeredAssemblies = [];
    private readonly List<MessageDescriptor> _descriptors = [];

    public TypeRegistry Registry { get; private set; }

    public InternalTypeRegistration()
    {
        // Enregistre toutes les assemblies déjà chargées au démarrage
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            RegisterAssembly(assembly);
    }

    public void RegisterAssembly(Assembly assembly)
    {
        if (!_registeredAssemblies.Add(assembly.FullName ?? assembly.GetName().Name!))
            return; // déjà enregistrée

        var discovered = ExtractDescriptors(assembly).ToList();
        if (discovered.Count == 0) return;

        _descriptors.AddRange(discovered);
        Registry = TypeRegistry.FromMessages(_descriptors);

        logger.LogInformation($"TypeRegistry: {discovered.Count} descriptor(s) ajouté(s) depuis '{assembly.GetName().Name}' (total: {_descriptors.Count})");
    }

    private static IEnumerable<MessageDescriptor> ExtractDescriptors(Assembly assembly)
    {
        IEnumerable<Type> types;
        try { types = assembly.GetTypes(); }
        catch { yield break; }

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface) continue;
            if (!typeof(IMessage).IsAssignableFrom(type)) continue;

            MessageDescriptor? descriptor = null;
            try
            {
                var prop = type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
                descriptor = prop?.GetValue(null) as MessageDescriptor;
            }
            catch { /* type inaccessible, on skip */ }

            if (descriptor is not null)
                yield return descriptor;
        }
    }

    public void Dispose() { }
}