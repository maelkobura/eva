namespace Eva.Node.Service.Functions;

public class FunctionDescriptor
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string[] Keywords { get; init; }
    public required string[] Authorization { get; init; }
    public required ParameterDescriptor[] Parameters { get; init; }
    public required Type ReturnType { get; init; }
    public required Func<object?[], Task<object?>> Invoke { get; init; }
}

public class ParameterDescriptor
{
    public required string Name { get; init; }
    public required Type Type { get; init; }
    public required bool IsRequired { get; init; }
}