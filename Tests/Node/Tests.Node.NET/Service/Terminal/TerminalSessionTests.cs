using System.Reflection;
using System.Text;
using Moq;
using Xunit;
using Eva.Commons.Messages;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Network;
using Eva.Node.Service;
using Eva.Node.Service.Calling;
using Eva.Node.Service.Functions;
using Eva.Node.Terminal;
using Google.Protobuf;
using Jint;
using Jint.Native;

namespace Tests.Node.NET.Service.Terminal;

public class TerminalSessionTests : IDisposable
{
    private readonly Mock<INetworkNodeManager> _nodeManagerMock;
    private readonly Mock<IServiceLoader> _serviceLoaderMock;
    private readonly Mock<IFunctionRegistry> _functionRegistryMock;
    private readonly Mock<IServiceRouter> _serviceRouterMock;
    private readonly Certificate _defaultCert;

    public TerminalSessionTests()
    {
        // Initialize global logger and clear DI
        EvaLogger.Init("Eva Commons Test");
        EvaSystem.Clear();

        // Setup mocks
        _nodeManagerMock = new Mock<INetworkNodeManager>();
        _serviceLoaderMock = new Mock<IServiceLoader>();
        _functionRegistryMock = new Mock<IFunctionRegistry>();
        _serviceRouterMock = new Mock<IServiceRouter>();

        // Default nodes setup (empty by default)
        _nodeManagerMock.Setup(m => m.Nodes).Returns(new List<NodeEntity>());

        // Register mocks as singletons
        EvaSystem.AddSingleton<INetworkNodeManager>(_nodeManagerMock.Object);
        EvaSystem.AddSingleton<IServiceLoader>(_serviceLoaderMock.Object);
        EvaSystem.AddSingleton<IFunctionRegistry>(_functionRegistryMock.Object);
        EvaSystem.AddSingleton<IServiceRouter>(_serviceRouterMock.Object);

        // Dummy certificate
        _defaultCert = CreateDummyCertificate("terminal-test-subject");
    }

    public void Dispose()
    {
    }

    [Fact]
    public void Constructor_InitializesEvaObjectWithServices()
    {
        // Arrange
        var localServiceName = "local-service";
        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(localServiceName));

        var functionPanel = new FunctionPanel
        {
            ServiceId = localServiceName,
            Functions = { new EvaFunctionDescriptor { Name = "testFunc" } }
        };
        _functionRegistryMock.Setup(r => r.GetPanel()).Returns(functionPanel);

        // Act
        using var session = new TerminalSession(_defaultCert);
        
        // Assert
        var result = session.Execute("typeof eva !== 'undefined' && typeof eva['local-service'] !== 'undefined'", _defaultCert);
        Assert.True(result.AsBoolean());
    }

    [Fact]
    public void Execute_CallsServiceFunction_WithCorrectParameters()
    {
        // Arrange
        var serviceName = "test-service";
        var functionName = "sayHello";
        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(serviceName));

        var functionPanel = new FunctionPanel
        {
            ServiceId = serviceName,
            Functions = { 
                new EvaFunctionDescriptor { 
                    Name = functionName, 
                    Parameters = { new EvaParameterDescriptor { Name = "name", Type = new ReturnType { Type = EvaType.String } } },
                    ReturnType = new ReturnType { Type = EvaType.String }
                } 
            }
        };
        _functionRegistryMock.Setup(r => r.GetPanel()).Returns(functionPanel);

        // Mock IServiceRouter.Call<byte[]> to handle the actual call
        byte[] expectedResponse = Encoding.UTF8.GetBytes("Hello World");
        _serviceRouterMock.Setup(r => r.Call<byte[]>(
            $"{serviceName}.{functionName}",
            _defaultCert,
            It.Is<object?[]>(p => p.Length == 1 && (string)p[0]! == "Alice")
        )).ReturnsAsync(expectedResponse);

        // Act
        using var session = new TerminalSession(_defaultCert);
        var script = $"eva['{serviceName}'].{functionName}('Alice')";
        var result = session.Execute(script, _defaultCert);

        // Assert
        Assert.Equal("Hello World", result.AsString());
        _serviceRouterMock.VerifyAll();
    }

    [Fact]
    public void Execute_ReturnsConvertedResult_FromServiceCall()
    {
        // Arrange
        var serviceName = "math-service";
        var functionName = "add";
        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(serviceName));

        var functionPanel = new FunctionPanel
        {
            ServiceId = serviceName,
            Functions = { 
                new EvaFunctionDescriptor { 
                    Name = functionName, 
                    Parameters = { 
                        new EvaParameterDescriptor { Name = "a", Type = new ReturnType { Type = EvaType.Int32 } },
                        new EvaParameterDescriptor { Name = "b", Type = new ReturnType { Type = EvaType.Int32 } }
                    },
                    ReturnType = new ReturnType { Type = EvaType.Int32 }
                } 
            }
        };
        _functionRegistryMock.Setup(r => r.GetPanel()).Returns(functionPanel);

        int resultValue = 42;
        byte[] encodedResult = BitConverter.GetBytes(resultValue);

        _serviceRouterMock.Setup(r => r.Call<byte[]>(
            $"{serviceName}.{functionName}",
            _defaultCert,
            It.IsAny<object?[]>()
        )).ReturnsAsync(encodedResult);

        // Act
        using var session = new TerminalSession(_defaultCert);
        var script = $"eva['{serviceName}'].{functionName}(10, 32)";
        var result = session.Execute(script, _defaultCert);

        // Assert
        Assert.Equal(42, result.AsNumber());
    }

    [Fact]
    public void Execute_MultipleNodes_AreAvailableInEvaObject()
    {
        // Arrange
        var localService = "local-node";
        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(localService));
        _functionRegistryMock.Setup(r => r.GetPanel()).Returns(new FunctionPanel { ServiceId = localService });

        var remoteNode = new NodeEntity("remote-node", "1.2.3.4");
        SetFunctionPanel(remoteNode, new FunctionPanel { 
            ServiceId = "remote-node", 
            Functions = { new EvaFunctionDescriptor { Name = "remoteFunc" } } 
        });

        _nodeManagerMock.Setup(m => m.Nodes).Returns(new List<NodeEntity> { remoteNode });

        // Act
        using var session = new TerminalSession(_defaultCert);
        
        // Assert
        var hasLocal = session.Execute("typeof eva['local-node'] !== 'undefined'", _defaultCert).AsBoolean();
        var hasRemote = session.Execute("typeof eva['remote-node'] !== 'undefined'", _defaultCert).AsBoolean();
        var hasRemoteFunc = session.Execute("typeof eva['remote-node'].remoteFunc !== 'undefined'", _defaultCert).AsBoolean();

        Assert.True(hasLocal, "Local node should be available");
        Assert.True(hasRemote, "Remote node should be available");
        Assert.True(hasRemoteFunc, "Remote function should be available");
    }

    private void SetFunctionPanel(NodeEntity node, FunctionPanel panel)
    {
        // Use reflection to set the private property FunctionPanel
        var property = typeof(NodeEntity).GetProperty("FunctionPanel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        property?.SetValue(node, panel);
    }

    private Certificate CreateDummyCertificate(string subject)
    {
        return new Certificate
        {
            Payload = new CertificatePayload
            {
                Content = new CertificateContent
                {
                    Subject = subject,
                    Expiration = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
                }
            }
        };
    }
}
