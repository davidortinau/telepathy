using Microsoft.Extensions.Logging;

namespace Telepathic.Services;

/// <summary>
/// Hybrid transcription service that prefers local ONNX but falls back to OpenAI Whisper
/// </summary>
public class HybridTranscriptionService : ITranscriptionService
{
    private readonly ILocalTranscriptionService _localTranscriptionService;
    private readonly WhisperTranscriptionService _openAiTranscriptionService;
    private readonly ILogger<HybridTranscriptionService> _logger;

    public HybridTranscriptionService(
        ILocalTranscriptionService localTranscriptionService,
        ILogger<HybridTranscriptionService> logger)
    {
        _localTranscriptionService = localTranscriptionService;
        _openAiTranscriptionService = new WhisperTranscriptionService();
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken ct)
    {
        // Try local ONNX first if available
        if (_localTranscriptionService.IsAvailable)
        {
            try
            {
                _logger.LogInformation("Attempting transcription using local ONNX model...");
                var localResult = await _localTranscriptionService.TranscribeAsync(audioFilePath, ct);
                _logger.LogInformation("Local ONNX transcription completed successfully");
                return localResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Local ONNX transcription failed, falling back to OpenAI");
            }
        }
        else
        {
            _logger.LogInformation("Local ONNX transcription not available, using OpenAI");
        }

        // Fallback to OpenAI Whisper
        _logger.LogInformation("Using OpenAI Whisper for transcription...");
        return await _openAiTranscriptionService.TranscribeAsync(audioFilePath, ct);
    }
}