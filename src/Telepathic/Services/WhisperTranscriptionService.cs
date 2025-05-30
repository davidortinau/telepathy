using Microsoft.Extensions.AI;
using OpenAI;
using Telepathic.Services;

namespace Telepathic.Services;

public class WhisperTranscriptionService : ITranscriptionService
{
    private readonly ISecureApiKeyService _secureApiKeyService;
    
    public WhisperTranscriptionService(ISecureApiKeyService secureApiKeyService)
    {
        _secureApiKeyService = secureApiKeyService;
    }
        
    public async Task<string> TranscribeAsync(string path, CancellationToken ct)
    {
        var openAiApiKey = await _secureApiKeyService.GetApiKeyAsync("openai_api_key");
        
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException("OpenAI API key not found. Please configure your API key in settings.");
        }
        
        var client = new OpenAIClient(openAiApiKey);
        
        try
        {
            await using var stream = File.OpenRead(path);
            var result = await client
                            .GetAudioClient("whisper-1")
                            .TranscribeAudioAsync(stream, "file.wav", cancellationToken: ct);

            return result.Value.Text.Trim();
        }
        catch (Exception ex)
        {
            // Will add better error handling in Phase 5
            throw new Exception($"Failed to transcribe audio: {ex.Message}", ex);
        }
    }
}
