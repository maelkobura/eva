using Eva.Commons.System;
using Eva.Commons.Util;

namespace Tests.Commons.System;

public interface ITestService : IDisposable
{
    int Value { get; }
}

public class TestService : ITestService
{
    public int Value { get; }

    public bool Disposed { get; private set; }

    public TestService(int value)
    {
        Value = value;
    }

    public void Dispose()
    {
        Disposed = true;
    }
}

public class EvaSystemTests
{
    
    public EvaSystemTests()
    {
        EvaLogger.Init("Eva Commons Test");
        EvaSystem.Clear(); // important vu le static
    }
    
    [Fact]
    public void AddSingleton_Should_Register_And_Return_Instance()
    {
        var instance = EvaSystem.AddSingleton<ITestService, TestService>(42);

        Assert.NotNull(instance);
        Assert.Equal(42, instance.Value);
        EvaSystem.Clear();
    }

    [Fact]
    public void Singleton_Should_Return_Same_Instance()
    {
        var instance1 = EvaSystem.AddSingleton<ITestService, TestService>(10);
        var instance2 = EvaSystem.Singleton<ITestService>();

        Assert.Same(instance1, instance2);
        EvaSystem.Clear();
    }

    [Fact]
    public void AddSingleton_Should_Throw_If_Already_Registered()
    {
        EvaSystem.AddSingleton<ITestService, TestService>(10);

        Assert.Throws<ArgumentException>(() =>
            EvaSystem.AddSingleton<ITestService, TestService>(2)
        );
        EvaSystem.Clear();
    }

    [Fact]
    public void Singleton_Should_Throw_If_Not_Registered()
    {
        Assert.Throws<ArgumentException>(() =>
            EvaSystem.Singleton<ITestService>()
        );
        EvaSystem.Clear();
    }

    [Fact]
    public void Clear_Should_Dispose_And_Remove_All_Singletons()
    {

        var instance = EvaSystem.AddSingleton<ITestService, TestService>(5);

        EvaSystem.Clear();

        Assert.True(instance.Disposed);

        Assert.Throws<ArgumentException>(() =>
            EvaSystem.Singleton<ITestService>()
        );
    }
}