using System.Text;
using System.Threading.Tasks;
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
using Google.Protobuf;

namespace Tests.Node.NET.Service.Calling;

public class InternalServiceRouterTests : IDisposable
{
    private readonly Mock<INetworkNodeManager> _nodeManagerMock;
    private readonly Mock<IServiceLoader> _serviceLoaderMock;
    private readonly Mock<IFunctionRegistry> _functionRegistryMock;
    private readonly InternalServiceRouter _router;
    private readonly Certificate _defaultCert;

    public InternalServiceRouterTests()
    {
        // Initialize global logger and clear DI
        EvaLogger.Init("Eva Commons Test");
        EvaSystem.Clear();

        // Setup mocks
        _nodeManagerMock = new Mock<INetworkNodeManager>();
        _serviceLoaderMock = new Mock<IServiceLoader>();
        _functionRegistryMock = new Mock<IFunctionRegistry>();

        // Register mocks as singletons
        _nodeManagerMock.Setup(m => m.Nodes).Returns(new List<NodeEntity>());
        EvaSystem.AddSingleton<INetworkNodeManager>(_nodeManagerMock.Object);
        EvaSystem.AddSingleton<IServiceLoader>(_serviceLoaderMock.Object);
        EvaSystem.AddSingleton<IFunctionRegistry>(_functionRegistryMock.Object);

        _router = new InternalServiceRouter();
        
        // Create a dummy certificate for testing
        _defaultCert = CreateDummyCertificate("test-subject");
    }

    public void Dispose()
    {
        _router.Dispose();
    }

    #region Loopback (Local) Tests

    [Fact]
    public async Task Call_Loopback_Success_ReturnsDeserializedValue()
    {
        // Arrange
        var nodeName = "local-node";
        var functionName = "GetStatus";
        var fullName = $"{nodeName}.{functionName}";
        var expectedResult = "Running";

        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(nodeName));
        
        var descriptor = new FunctionDescriptor
        {
            Name = functionName,
            Description = "Test function",
            Keywords = [],
            Authorization = [],
            Parameters = [new ParameterDescriptor { Name = "param1", Type = typeof(string), IsRequired = true }],
            ReturnType = typeof(string),
            Invoke = args => Task.FromResult<object?>(expectedResult),
            Depreciated = false,
            Weight = 0,
            Flags = []
        };
        
        _functionRegistryMock.Setup(r => r.Get(functionName)).Returns(descriptor);

        // Act
        var result = await _router.Call<string>(fullName, _defaultCert, "some-input");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task Call_Loopback_FunctionNotFound_ThrowsException()
    {
        // Arrange
        var nodeName = "local-node";
        var functionName = "UnknownFunc";
        var fullName = $"{nodeName}.{functionName}";

        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(nodeName));
        _functionRegistryMock.Setup(r => r.Get(functionName)).Returns((FunctionDescriptor?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _router.Call<string>(fullName, _defaultCert));
        Assert.Equal($"Function '{functionName}' not found locally", ex.Message);
    }

    [Fact]
    public async Task Call_Loopback_ExecutionFailure_ThrowsException()
    {
        // Arrange
        var nodeName = "local-node";
        var functionName = "FailFunc";
        var fullName = $"{nodeName}.{functionName}";
        var errorMessage = "Something went wrong";

        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(nodeName));
        
        var descriptor = new FunctionDescriptor
        {
            Name = functionName,
            Description = "Test failure",
            Keywords = [],
            Authorization = [],
            Parameters = [],
            ReturnType = typeof(int),
            Invoke = args => throw new Exception(errorMessage),
            Depreciated = false,
            Weight = 0,
            Flags = []
        };
        
        _functionRegistryMock.Setup(r => r.Get(functionName)).Returns(descriptor);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _router.Call<int>(fullName, _defaultCert));
        Assert.Contains(errorMessage, ex.Message);
    }

    #endregion

    #region Remote (Network) Tests

    [Fact]
    public async Task Call_Remote_Success_ReturnsValue()
    {
        // Arrange
        var remoteNodeName = "remote-node";
        var functionName = "Multiply";
        var fullName = $"{remoteNodeName}.{functionName}";
        var expectedValue = 42;

        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription("local-node"));
        
        var nodeMock = new Mock<NodeEntity>(remoteNodeName, "127.0.0.1");
        nodeMock.Setup(n => n.GetFunction(functionName)).Returns(new EvaFunctionDescriptor
        {
            Name = functionName,
            Parameters = { new EvaParameterDescriptor { Name = "a", Type = new ReturnType { Type = EvaType.Int32 } } }
        });
        
        var invokeResponse = new InvokeResponse
        {
            Success = true,
            Result = ByteString.CopyFrom(BitConverter.GetBytes(expectedValue))
        };
        
        nodeMock.Setup(n => n.InvokeAsync(functionName, It.IsAny<Dictionary<string, ByteString>>(), _defaultCert))
                .ReturnsAsync(invokeResponse);

        _nodeManagerMock.Setup(m => m.Nodes).Returns([nodeMock.Object]);

        // Act
        var result = await _router.Call<int>(fullName, _defaultCert, 10);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task Call_Remote_NodeNotFound_ThrowsException()
    {
        // Arrange
        var remoteNodeName = "unknown-node";
        var fullName = $"{remoteNodeName}.SomeFunc";

        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription("local-node"));
        _nodeManagerMock.Setup(m => m.Nodes).Returns([]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _router.Call<string>(fullName, _defaultCert));
        Assert.Equal($"Node '{remoteNodeName}' not found", ex.Message);
    }

    [Fact]
    public async Task Call_Remote_FunctionNotFound_ThrowsException()
    {
        // Arrange
        var remoteNodeName = "remote-node";
        var functionName = "MissingFunc";
        var fullName = $"{remoteNodeName}.{functionName}";

        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription("local-node"));
        
        var nodeMock = new Mock<NodeEntity>(remoteNodeName, "127.0.0.1");
        nodeMock.Setup(n => n.GetFunction(functionName)).Returns((EvaFunctionDescriptor?)null);

        _nodeManagerMock.Setup(m => m.Nodes).Returns([nodeMock.Object]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _router.Call<string>(fullName, _defaultCert));
        Assert.Equal($"Function '{functionName}' not found on node '{remoteNodeName}'", ex.Message);
    }

    [Fact]
    public async Task Call_Remote_InvokeFailure_ThrowsException()
    {
        // Arrange
        var remoteNodeName = "remote-node";
        var functionName = "BadFunc";
        var fullName = $"{remoteNodeName}.{functionName}";
        var remoteError = "Remote crash";

        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription("local-node"));
        
        var nodeMock = new Mock<NodeEntity>(remoteNodeName, "127.0.0.1");
        nodeMock.Setup(n => n.GetFunction(functionName)).Returns(new EvaFunctionDescriptor { Name = functionName });
        
        nodeMock.Setup(n => n.InvokeAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, ByteString>>(), It.IsAny<Certificate>()))
                .ReturnsAsync(new InvokeResponse { Success = false, Error = remoteError });

        _nodeManagerMock.Setup(m => m.Nodes).Returns([nodeMock.Object]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _router.Call<string>(fullName, _defaultCert));
        Assert.Equal(remoteError, ex.Message);
    }

    #endregion

    #region Parameter Serialization Tests

    [Theory]
    [InlineData("hello")]
    [InlineData(123)]
    [InlineData(456L)]
    [InlineData(true)]
    [InlineData(1.5f)]
    [InlineData(2.5d)]
    public async Task Call_TypesCheck_CorrectlySerializesAndDeserializes(object value)
    {
        // Arrange
        var type = value.GetType();
        var nodeName = "local";
        var fullName = $"{nodeName}.Func";
        _serviceLoaderMock.Setup(s => s.Description).Returns(new ServiceDescription(nodeName));

        // We use Reflection to call the generic method Call<T>
        var method = _router.GetType().GetMethod("Call", [typeof(string), typeof(Certificate), typeof(object?[])]);
        var genericMethod = method?.MakeGenericMethod(type);

        var descriptor = new FunctionDescriptor
        {
            Name = "Func",
            Description = "", Keywords = [], Authorization = [],
            Parameters = [new ParameterDescriptor { Name = "p", Type = type, IsRequired = true }],
            ReturnType = type,
            Invoke = args => Task.FromResult(args[0]),
            Depreciated = false, Weight = 0, Flags = []
        };
        _functionRegistryMock.Setup(r => r.Get("Func")).Returns(descriptor);

        // Act
        var taskResult = (Task)genericMethod?.Invoke(_router, [fullName, _defaultCert, new object?[] { value }])!;
        await taskResult;
        var result = ((dynamic)taskResult).Result;

        // Assert
        Assert.Equal(value, result);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task Call_InvalidFullName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _router.Call<string>("InvalidNameNoDot", _defaultCert));
    }

    #endregion

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
