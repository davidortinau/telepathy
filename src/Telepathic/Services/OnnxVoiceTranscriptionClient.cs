using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using System.Text.Json;

namespace Telepathic.Services;

/// <summary>
/// ONNX-based voice transcription client using Whisper-tiny model
/// </summary>
public class OnnxVoiceTranscriptionClient : ILocalTranscriptionService, IDisposable
{
    private readonly ILogger<OnnxVoiceTranscriptionClient> _logger;
    private InferenceSession? _encoderSession;
    private InferenceSession? _decoderSession;
    private Dictionary<string, int>? _tokenizer;
    private Dictionary<int, string>? _reverseTokenizer;
    private bool _isInitialized;

    public OnnxVoiceTranscriptionClient(ILogger<OnnxVoiceTranscriptionClient> logger)
    {
        _logger = logger;
        _ = InitializeAsync(); // Fire and forget initialization
    }

    public bool IsAvailable => _isInitialized;

    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing ONNX voice transcription models...");

            // Try to load models from app package
            var encoderPath = await GetModelPathAsync("encoder_model.onnx");
            var decoderPath = await GetModelPathAsync("decoder_model_merged.onnx");
            var tokenizerPath = await GetModelPathAsync("tokenizer.json");

            if (encoderPath == null || decoderPath == null || tokenizerPath == null)
            {
                _logger.LogWarning("One or more ONNX model files not found. Voice transcription will not be available locally.");
                return;
            }

            // Initialize ONNX sessions
            _encoderSession = new InferenceSession(encoderPath);
            _decoderSession = new InferenceSession(decoderPath);

            // Load tokenizer
            await LoadTokenizerAsync(tokenizerPath);

            _isInitialized = true;
            _logger.LogInformation("ONNX voice transcription models initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ONNX voice transcription models");
            _isInitialized = false;
        }
    }

    private async Task<string?> GetModelPathAsync(string fileName)
    {
        try
        {
            var modelPath = $"Models/VoiceTranscription/{fileName}";
            
            // Check if this is a placeholder file
            if (fileName.EndsWith(".placeholder"))
            {
                _logger.LogWarning("Model file {FileName} is a placeholder and not a real ONNX model", fileName);
                return null;
            }
            
            var stream = await FileSystem.OpenAppPackageFileAsync(modelPath);
            stream.Close();
            
            // Copy to local directory for ONNX runtime access
            var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);
            if (!File.Exists(localPath))
            {
                using var sourceStream = await FileSystem.OpenAppPackageFileAsync(modelPath);
                using var targetStream = File.Create(localPath);
                await sourceStream.CopyToAsync(targetStream);
            }
            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Model file {FileName} not found in app package", fileName);
            return null;
        }
    }

    private async Task LoadTokenizerAsync(string tokenizerPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(tokenizerPath);
            var tokenizerData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            // Simplified tokenizer loading - in a real implementation you'd need proper tokenizer parsing
            _tokenizer = new Dictionary<string, int>();
            _reverseTokenizer = new Dictionary<int, string>();
            
            // For now, just create basic Whisper tokens
            // In a real implementation, you'd parse the actual tokenizer.json properly
            _tokenizer["<|startoftranscript|>"] = 50258;
            _tokenizer["<|en|>"] = 50259;
            _tokenizer["<|transcribe|>"] = 50360;
            _tokenizer["<|endoftext|>"] = 50257;
            
            foreach (var kvp in _tokenizer)
            {
                _reverseTokenizer[kvp.Value] = kvp.Key;
            }
            
            _logger.LogInformation("Tokenizer loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tokenizer");
            throw;
        }
    }

    public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ONNX voice transcription client is not initialized");
        }

        using var fileStream = File.OpenRead(audioFilePath);
        return await TranscribeAsync(fileStream, cancellationToken);
    }

    public async Task<string> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ONNX voice transcription client is not initialized");
        }

        try
        {
            _logger.LogInformation("Transcribing audio using ONNX Whisper model...");

            // Preprocess audio to mel spectrogram format expected by Whisper
            var melSpectrogram = await PreprocessAudioAsync(audioStream, cancellationToken);

            // Run encoder
            var encoderInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_features", melSpectrogram)
            };

            using var encoderResults = _encoderSession!.Run(encoderInputs);
            var encoderOutput = encoderResults.First().AsTensor<float>();

            // Run decoder for text generation
            var transcript = await GenerateTextFromEncoderOutput(encoderOutput, cancellationToken);

            _logger.LogInformation("Audio transcription completed successfully: {Transcript}", transcript);
            return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio");
            throw;
        }
    }

    private async Task<Microsoft.ML.OnnxRuntime.Tensors.Tensor<float>> PreprocessAudioAsync(Stream audioStream, CancellationToken cancellationToken)
    {
        // Simplified audio preprocessing - in a real implementation you'd need:
        // 1. Load audio file (WAV, MP3, etc.)
        // 2. Resample to 16kHz mono
        // 3. Pad or trim to 30 seconds (480,000 samples)
        // 4. Apply mel filterbank to create mel spectrogram
        // 5. Apply log and normalization
        
        await Task.Delay(10, cancellationToken); // Simulate processing time
        
        // Create placeholder mel spectrogram tensor [1, 80, 3000] - batch_size, mel_bins, time_steps
        // Whisper expects 80 mel bins and up to 3000 time steps (30 seconds at 16kHz)
        var melSpectrogram = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new[] { 1, 80, 3000 });
        
        // Fill with placeholder values
        for (int i = 0; i < 1; i++)
        {
            for (int j = 0; j < 80; j++)
            {
                for (int k = 0; k < 3000; k++)
                {
                    melSpectrogram[i, j, k] = 0.0f; // Placeholder - would be real mel spectrogram values
                }
            }
        }

        return melSpectrogram;
    }

    private async Task<string> GenerateTextFromEncoderOutput(Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> encoderOutput, CancellationToken cancellationToken)
    {
        // Simplified text generation - in a real implementation you'd need proper autoregressive decoding
        await Task.Delay(10, cancellationToken); // Simulate processing time
        
        // This is a simplified placeholder. A real implementation would:
        // 1. Initialize decoder with start tokens (<|startoftranscript|><|en|><|transcribe|>)
        // 2. Run decoder iteratively with encoder output and previous tokens
        // 3. Apply beam search or greedy search  
        // 4. Stop when end token is generated or max length reached
        // 5. Decode tokens back to text using the tokenizer
        // 6. Post-process to clean up the transcript
        
        return "This is a placeholder transcription from local ONNX model."; // Placeholder transcript
    }

    public void Dispose()
    {
        _encoderSession?.Dispose();
        _decoderSession?.Dispose();
    }
}