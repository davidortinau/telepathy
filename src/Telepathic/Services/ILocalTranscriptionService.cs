namespace Telepathic.Services;

/// <summary>
/// Interface for local voice transcription services
/// </summary>
public interface ILocalTranscriptionService
{
    /// <summary>
    /// Transcribe audio using local ONNX model
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcribed text</returns>
    Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transcribe audio using local ONNX model
    /// </summary>
    /// <param name="audioStream">Audio stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcribed text</returns>
    Task<string> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the local transcription client is available for use
    /// </summary>
    bool IsAvailable { get; }
}