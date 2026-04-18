using System.Reflection;
using Google.Protobuf.Reflection;

namespace Eva.Node.Types;

public interface ITypeRegistration : IDisposable
{
    TypeRegistry Registry { get; }
    void RegisterAssembly(Assembly assembly);
}