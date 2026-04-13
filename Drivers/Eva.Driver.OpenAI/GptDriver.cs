using System.ClientModel;
using Eva.Drivers.Abstractions.Drivers;
using Eva.Drivers.Abstractions.Messages;
using OpenAI;
using OpenAI.Chat; // Requis pour ChatClient et ChatMessage

namespace Eva.Drivers.OpenAI;

public class GptDriver : LargeLanguageModelDriver 
{
    public string Name { get; set; } // Utilisé comme ID du modèle (ex: "gpt-4")
    public Dictionary<string, string> Configuration { get; set; }

    private string _serverUrl;
    private string _apiKey;
    private OpenAIClient _client;
    private ChatClient _chatClient;

    public void Initialize()
    {
        Configuration.TryGetValue("server", out _serverUrl);
        Configuration.TryGetValue("apiKey", out _apiKey);
        
        if (string.IsNullOrEmpty(_serverUrl))
        {
            _client = new OpenAIClient(new ApiKeyCredential(_apiKey));
        }
        else
        {
            _client = new OpenAIClient(new ApiKeyCredential(_apiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(_serverUrl)
            });
        }

        // On initialise le ChatClient spécifique au modèle
        _chatClient = _client.GetChatClient(Name);
    }

    public void Shutdown() { }

    public async Task<LlmResponse> CompleteAsync(IReadOnlyList<LlmMessage> messages, LlmOptions? options = null, CancellationToken cancellationToken = default)
    {
        var chatMessages = MapMessages(messages);
        var chatOptions = MapOptions(options);

        ClientResult<ChatCompletion> result = await _chatClient.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);
    
        var completion = result.Value;

        return new LlmResponse 
        {
            Content = completion.Content[0].Text,
            PromptTokens = completion.Usage.InputTokenCount,
            CompletionTokens = completion.Usage.OutputTokenCount 
        };
    }

    public async IAsyncEnumerable<string> StreamAsync(IReadOnlyList<LlmMessage> messages, LlmOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatMessages = MapMessages(messages);
        var chatOptions = MapOptions(options);

        AsyncCollectionResult<StreamingChatCompletionUpdate> updates = _chatClient.CompleteChatStreamingAsync(chatMessages, chatOptions, cancellationToken);

        await foreach (var update in updates)
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return part.Text;
                }
            }
        }
    }

    // --- Helpers de mapping ---

    private List<ChatMessage> MapMessages(IReadOnlyList<LlmMessage> messages)
    {
        return messages.Select<LlmMessage, ChatMessage>(m =>
        {
            switch (m.Role.ToLower())
            {
                case "system":
                    return ChatMessage.CreateSystemMessage(m.Content);
                case "assistant":
                    return ChatMessage.CreateAssistantMessage(m.Content);
                default:
                    return ChatMessage.CreateUserMessage(m.Content);
            }
        }).ToList();
    }

    private ChatCompletionOptions MapOptions(LlmOptions? options)
    {
        if (options == null) return null;

        return new ChatCompletionOptions
        {
            Temperature = options.Temperature,
            MaxOutputTokenCount = options.MaxTokens > 0 ? options.MaxTokens : null
        };
    }
}