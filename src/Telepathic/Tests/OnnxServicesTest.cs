using Microsoft.Extensions.Logging;
using Telepathic.Services;

namespace Telepathic.Tests;

/// <summary>
/// Simple test to verify ONNX service implementations
/// </summary>
public class OnnxServicesTest
{
    public static async Task TestOnnxServicesAsync()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // Test Image Caption Client
        Console.WriteLine("Testing ONNX Image Caption Client...");
        var imageCaptionLogger = loggerFactory.CreateLogger<OnnxImageCaptionClient>();
        var imageCaptionClient = new OnnxImageCaptionClient(imageCaptionLogger);
        
        await Task.Delay(1000); // Allow initialization
        
        Console.WriteLine($"Image Caption Client Available: {imageCaptionClient.IsAvailable}");
        
        // Test Voice Transcription Client
        Console.WriteLine("Testing ONNX Voice Transcription Client...");
        var voiceTranscriptionLogger = loggerFactory.CreateLogger<OnnxVoiceTranscriptionClient>();
        var voiceTranscriptionClient = new OnnxVoiceTranscriptionClient(voiceTranscriptionLogger);
        
        await Task.Delay(1000); // Allow initialization
        
        Console.WriteLine($"Voice Transcription Client Available: {voiceTranscriptionClient.IsAvailable}");
        
        // Test Hybrid Transcription Service
        Console.WriteLine("Testing Hybrid Transcription Service...");
        var hybridLogger = loggerFactory.CreateLogger<HybridTranscriptionService>();
        var hybridService = new HybridTranscriptionService(voiceTranscriptionClient, hybridLogger);
        
        Console.WriteLine("All ONNX services instantiated successfully!");
        
        // Cleanup
        imageCaptionClient.Dispose();
        voiceTranscriptionClient.Dispose();
    }
}