using Eva.Drivers.Abstractions.Messages;

namespace Eva.Drivers.Abstractions.Drivers;

public interface LargeLanguageModelDriver : EvaDriver {
    /// <summary>
    /// Sends a conversation to the model and returns the full response.
    /// </summary>
    Task<LlmResponse> CompleteAsync(IReadOnlyList<LlmMessage> messages, LlmOptions? options = null, CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Streams the response token by token.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(IReadOnlyList<LlmMessage> messages, LlmOptions? options = null, CancellationToken cancellationToken = default);
}
