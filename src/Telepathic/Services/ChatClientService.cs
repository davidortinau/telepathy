using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Telepathic.Tools;
using OpenAI;
using Azure.AI.OpenAI;

namespace Telepathic.Services;

/// <summary>
/// Interface for a service that manages chat client creation and updates
/// </summary>
public interface IChatClientService
{
    /// <summary>
    /// Gets the current chat client instance
    /// </summary>
    /// <returns>The current IChatClient instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when the chat client has not been initialized</exception>
    IChatClient GetClient();
    
    /// <summary>
    /// Gets the MCP tools that can be used with the chat client
    /// </summary>
    /// <returns>A list of available MCP tools</returns>
    Task<IList<object>> GetMcpToolsAsync();
    
    /// <summary>
    /// Gets a response from the chat client with MCP tools included
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="prompt">The prompt to send to the chat client</param>
    /// <returns>The chat response</returns>
    Task<ChatResponse<T>> GetResponseWithToolsAsync<T>(string prompt);
    
    /// <summary>
    /// Updates the chat client with a new API key
    /// </summary>
    /// <param name="apiKey">The OpenAI API key</param>
    /// <param name="model">The model to use (defaults to o4-mini)</param>
    void UpdateClient(string apiKey, string model = "o4-mini");

    /// <summary>
    /// Updates the chat client with provider-specific settings
    /// </summary>
    /// <param name="apiKey">The API key for the provider</param>
    /// <param name="provider">The provider type (e.g., "openai", "foundry")</param>
    /// <param name="endpoint">The endpoint URL (required for foundry, optional for others)</param>
    /// <param name="model">The model to use</param>
    /// <param name="deployment">The deployment name (for Azure OpenAI, if applicable)</param>
    void UpdateClient(string apiKey, string provider, string? endpoint = null, string model = "o4-mini", string deployment= "o4-mini");
    
    /// <summary>
    /// Checks if the client is initialized and ready to use
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Service that manages the chat client and allows updating it at runtime
/// </summary>
public class ChatClientService : IChatClientService
{
    private IChatClient? _chatClient;
    private readonly ILogger _logger;
    private readonly LocationTools _locationTools;
    private IList<object>? _cachedTools;

    public ChatClientService(ILogger<ChatClientService> logger, LocationTools locationTools)
    {
        _logger = logger;
        _locationTools = locationTools;
        
        // Try to initialize from preferences if available
        // Check for Foundry settings first (higher priority if both are configured)
        var foundryEndpoint = Preferences.Default.Get("foundry_endpoint", string.Empty);
        var foundryApiKey = Preferences.Default.Get("foundry_api_key", string.Empty);
        var foundryModel = Preferences.Default.Get("foundry_model", "o4-mini");
        var foundryDeploymentName = Preferences.Default.Get("foundry_deployment_name", "o4-mini");
        var openAiApiKey = Preferences.Default.Get("openai_api_key", string.Empty);
        
        if (!string.IsNullOrEmpty(foundryEndpoint) && !string.IsNullOrEmpty(foundryApiKey) && !string.IsNullOrEmpty(foundryDeploymentName) && !string.IsNullOrEmpty(foundryModel))
        {
            UpdateClient(foundryApiKey, "foundry", foundryEndpoint, foundryModel, foundryDeploymentName);
        }
        else if (!string.IsNullOrEmpty(openAiApiKey))
        {
            UpdateClient(openAiApiKey);
        }
    }

    public IChatClient GetClient()
    {
        return _chatClient ?? throw new InvalidOperationException("Chat client has not been initialized. Please provide an API key first.");
    }

    public bool IsInitialized => _chatClient != null;
    
    /// <summary>
    /// Gets the available MCP tools that can be used with the chat client
    /// </summary>
    public Task<IList<object>> GetMcpToolsAsync()
    {
        if (_cachedTools != null)
        {
            return Task.FromResult(_cachedTools);
        }
        
        try
        {
            // Directly use LocationTools without going through McpService
            _cachedTools = new List<object> { _locationTools };
            return Task.FromResult(_cachedTools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MCP tools");
            return Task.FromResult<IList<object>>(new List<object>());
        }
    }
    
    /// <summary>
    /// Gets a response from the chat client with MCP tools included
    /// </summary>
    public async Task<ChatResponse<T>> GetResponseWithToolsAsync<T>(string prompt)
    {
        var client = GetClient();
        var tools = await GetMcpToolsAsync();
        
        // Create chat options with tools included
        var options = new ChatOptions();
        
        // Don't use the tools directly - instead let MCP system handle registration
        // The LocationTools is already registered with the MCP server
        
        _logger.LogInformation("Calling chat client with location tools available");
        return await client.GetResponseAsync<T>(prompt, options);
    }

    public void UpdateClient(string apiKey, string model = "o4-mini")
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Attempted to update chat client with empty API key");
            _chatClient = null;
            return;
        }

        try
        {
            var openAIClient = new OpenAIClient(apiKey);
            _chatClient = openAIClient.GetChatClient(model: model).AsIChatClient();
            _chatClient = new LoggingChatClient(_chatClient, _logger);

            _chatClient = new ChatClientBuilder(_chatClient)
            .ConfigureOptions(options =>
            {
                options.Tools ??= [];
                options.Tools.Add(AIFunctionFactory.Create(_locationTools.IsNearby));
            })
            .UseFunctionInvocation()
            .Build();
            
            
            // Clear cached tools when client is updated
            _cachedTools = null;
            
            _logger.LogInformation("Chat client successfully initialized with model: {Model}", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat client");
            _chatClient = null;
            throw;
        }
    }

    public void UpdateClient(string apiKey, string provider, string? endpoint = null, string model = "o4-mini", string deployment = "o4-mini")
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Attempted to update chat client with empty API key");
            _chatClient = null;
            return;
        }

        try
        {
            switch (provider.ToLowerInvariant())
            {
                case "foundry":
                    if (string.IsNullOrEmpty(endpoint))
                    {
                        throw new ArgumentException("Foundry provider requires an endpoint URL", nameof(endpoint));
                    }
                    _logger.LogInformation("Initializing Foundry chat client with endpoint: {Endpoint}", endpoint);
                    
                    // For Foundry, create OpenAI client that points to the Foundry endpoint
                    // Most Foundry services are OpenAI-compatible
                    var foundryClient = new AzureOpenAIClient(new Uri(endpoint),new System.ClientModel.ApiKeyCredential(apiKey));
                    _chatClient = foundryClient.GetChatClient(deployment).AsIChatClient();
                    break;
                    
                case "openai":
                default:
                    // Use OpenAI client (existing logic)
                    var openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey));
                    _chatClient = openAIClient.GetChatClient(model: model).AsIChatClient();
                    break;
            }

            _chatClient = new LoggingChatClient(_chatClient, _logger);

            _chatClient = new ChatClientBuilder(_chatClient)
            .ConfigureOptions(options =>
            {
                options.Tools ??= [];
                options.Tools.Add(AIFunctionFactory.Create(_locationTools.IsNearby));
            })
            .UseFunctionInvocation()
            .Build();
            
            // Clear cached tools when client is updated
            _cachedTools = null;
            
            _logger.LogInformation("Chat client successfully initialized with provider: {Provider}, model: {Model}", provider, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat client with provider: {Provider}", provider);
            _chatClient = null;
            throw;
        }
    }
}
