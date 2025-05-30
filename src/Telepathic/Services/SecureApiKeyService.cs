using Microsoft.Extensions.Logging;

namespace Telepathic.Services;

/// <summary>
/// Service for securely storing and retrieving API keys using SecureStorage
/// </summary>
public interface ISecureApiKeyService
{
    /// <summary>
    /// Gets an API key securely
    /// </summary>
    /// <param name="keyName">The name of the API key</param>
    /// <returns>The API key value or empty string if not found</returns>
    Task<string> GetApiKeyAsync(string keyName);
    
    /// <summary>
    /// Sets an API key securely
    /// </summary>
    /// <param name="keyName">The name of the API key</param>
    /// <param name="value">The API key value</param>
    Task SetApiKeyAsync(string keyName, string value);
    
    /// <summary>
    /// Removes an API key
    /// </summary>
    /// <param name="keyName">The name of the API key</param>
    Task RemoveApiKeyAsync(string keyName);
}

/// <summary>
/// Implementation of secure API key storage using SecureStorage
/// </summary>
public class SecureApiKeyService : ISecureApiKeyService
{
    private readonly ILogger<SecureApiKeyService> _logger;
    
    public SecureApiKeyService(ILogger<SecureApiKeyService> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> GetApiKeyAsync(string keyName)
    {
        try
        {
            var value = await SecureStorage.Default.GetAsync(keyName);
            return value ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve API key: {KeyName}", keyName);
            return string.Empty;
        }
    }
    
    public async Task SetApiKeyAsync(string keyName, string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                await RemoveApiKeyAsync(keyName);
                return;
            }
            
            await SecureStorage.Default.SetAsync(keyName, value);
            _logger.LogInformation("API key securely stored: {KeyName}", keyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store API key: {KeyName}", keyName);
            throw;
        }
    }
    
    public async Task RemoveApiKeyAsync(string keyName)
    {
        try
        {
            SecureStorage.Default.Remove(keyName);
            _logger.LogInformation("API key removed: {KeyName}", keyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove API key: {KeyName}", keyName);
            throw;
        }
    }
}