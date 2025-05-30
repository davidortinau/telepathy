using Microsoft.Extensions.Logging;
using Telepathic.Services;

namespace Telepathic.Tests;

/// <summary>
/// Simple test to verify SecureApiKeyService functionality
/// This would be moved to a proper test project in a real scenario
/// </summary>
public class SecureApiKeyServiceTests
{
    private readonly ISecureApiKeyService _service;
    private readonly ILogger<SecureApiKeyService> _logger;

    public SecureApiKeyServiceTests()
    {
        // In a real test, we'd use a mock logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<SecureApiKeyService>();
        _service = new SecureApiKeyService(_logger);
    }

    /// <summary>
    /// Test basic set and get operations
    /// </summary>
    public async Task TestBasicOperations()
    {
        const string testKey = "test_api_key";
        const string testValue = "sk-test123456789";

        try
        {
            // Test setting a key
            await _service.SetApiKeyAsync(testKey, testValue);
            
            // Test retrieving the key
            var retrievedValue = await _service.GetApiKeyAsync(testKey);
            
            if (retrievedValue != testValue)
            {
                throw new Exception($"Expected '{testValue}', got '{retrievedValue}'");
            }
            
            // Test removing the key
            await _service.RemoveApiKeyAsync(testKey);
            
            // Verify it's gone
            var removedValue = await _service.GetApiKeyAsync(testKey);
            if (!string.IsNullOrEmpty(removedValue))
            {
                throw new Exception($"Expected empty string after removal, got '{removedValue}'");
            }
            
            Console.WriteLine("✅ SecureApiKeyService basic operations test passed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SecureApiKeyService test failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Test handling of empty/null values
    /// </summary>
    public async Task TestEdgeCases()
    {
        const string testKey = "edge_case_key";

        try
        {
            // Test setting empty value (should remove the key)
            await _service.SetApiKeyAsync(testKey, "");
            var result = await _service.GetApiKeyAsync(testKey);
            
            if (!string.IsNullOrEmpty(result))
            {
                throw new Exception($"Expected empty result for empty value, got '{result}'");
            }
            
            // Test getting non-existent key
            var nonExistent = await _service.GetApiKeyAsync("non_existent_key");
            if (!string.IsNullOrEmpty(nonExistent))
            {
                throw new Exception($"Expected empty result for non-existent key, got '{nonExistent}'");
            }
            
            Console.WriteLine("✅ SecureApiKeyService edge cases test passed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SecureApiKeyService edge cases test failed: {ex.Message}");
            throw;
        }
    }
}