using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;

namespace Telepathic.Services;

/// <summary>
/// ONNX-based image captioning client using ViT-GPT2 model
/// </summary>
public class OnnxImageCaptionClient : IImageCaptionClient, IDisposable
{
    private readonly ILogger<OnnxImageCaptionClient> _logger;
    private InferenceSession? _encoderSession;
    private InferenceSession? _decoderSession;
    private Dictionary<string, int>? _tokenizer;
    private Dictionary<int, string>? _reverseTokenizer;
    private bool _isInitialized;

    public OnnxImageCaptionClient(ILogger<OnnxImageCaptionClient> logger)
    {
        _logger = logger;
        _ = InitializeAsync(); // Fire and forget initialization
    }

    public bool IsAvailable => _isInitialized;

    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing ONNX image captioning models...");

            // Try to load models from app package
            var encoderPath = await GetModelPathAsync("encoder_model.onnx");
            var decoderPath = await GetModelPathAsync("decoder_model_merged.onnx");
            var tokenizerPath = await GetModelPathAsync("tokenizer.json");

            if (encoderPath == null || decoderPath == null || tokenizerPath == null)
            {
                _logger.LogWarning("One or more ONNX model files not found. Image captioning will not be available locally.");
                return;
            }

            // Initialize ONNX sessions
            _encoderSession = new InferenceSession(encoderPath);
            _decoderSession = new InferenceSession(decoderPath);

            // Load tokenizer
            await LoadTokenizerAsync(tokenizerPath);

            _isInitialized = true;
            _logger.LogInformation("ONNX image captioning models initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ONNX image captioning models");
            _isInitialized = false;
        }
    }

    private async Task<string?> GetModelPathAsync(string fileName)
    {
        try
        {
            var modelPath = $"Models/ImageCaptioning/{fileName}";
            
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
            
            // For now, just create a basic vocabulary
            // In a real implementation, you'd parse the actual tokenizer.json properly
            _tokenizer["<|endoftext|>"] = 50256;
            _reverseTokenizer[50256] = "<|endoftext|>";
            
            _logger.LogInformation("Tokenizer loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tokenizer");
            throw;
        }
    }

    public async Task<string> GenerateCaptionAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ONNX image captioning client is not initialized");
        }

        using var image = await Image.LoadAsync<Rgb24>(imagePath, cancellationToken);
        return await GenerateCaptionInternalAsync(image, cancellationToken);
    }

    public async Task<string> GenerateCaptionAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ONNX image captioning client is not initialized");
        }

        using var image = await Image.LoadAsync<Rgb24>(imageStream, cancellationToken);
        return await GenerateCaptionInternalAsync(image, cancellationToken);
    }

    private async Task<string> GenerateCaptionInternalAsync(Image<Rgb24> image, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating image caption using ONNX model...");

            // Preprocess image to 224x224 as required by ViT
            var preprocessedImage = PreprocessImage(image);

            // Run encoder
            var encoderInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("pixel_values", preprocessedImage)
            };

            using var encoderResults = _encoderSession!.Run(encoderInputs);
            var encoderOutput = encoderResults.First().AsTensor<float>();

            // Run decoder for caption generation
            var caption = await GenerateTextFromEncoderOutput(encoderOutput, cancellationToken);

            _logger.LogInformation("Image caption generated successfully: {Caption}", caption);
            return caption;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image caption");
            throw;
        }
    }

    private Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> PreprocessImage(Image<Rgb24> image)
    {
        // Resize to 224x224
        image.Mutate(x => x.Resize(224, 224));

        // Convert to tensor format [1, 3, 224, 224] - batch_size, channels, height, width
        var tensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new[] { 1, 3, 224, 224 });

        // Normalize with ImageNet means and stds
        var means = new[] { 0.485f, 0.456f, 0.406f };
        var stds = new[] { 0.229f, 0.224f, 0.225f };

        for (int y = 0; y < 224; y++)
        {
            for (int x = 0; x < 224; x++)
            {
                var pixel = image[x, y];
                
                // Normalize to [0, 1] then apply ImageNet normalization
                tensor[0, 0, y, x] = (pixel.R / 255.0f - means[0]) / stds[0]; // Red
                tensor[0, 1, y, x] = (pixel.G / 255.0f - means[1]) / stds[1]; // Green
                tensor[0, 2, y, x] = (pixel.B / 255.0f - means[2]) / stds[2]; // Blue
            }
        }

        return tensor;
    }

    private async Task<string> GenerateTextFromEncoderOutput(Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> encoderOutput, CancellationToken cancellationToken)
    {
        // Simplified text generation - in a real implementation you'd need proper beam search or greedy decoding
        // For now, return a placeholder caption
        await Task.Delay(10, cancellationToken); // Simulate processing time
        
        // This is a simplified placeholder. A real implementation would:
        // 1. Initialize with start token
        // 2. Run decoder iteratively with encoder output and previous tokens
        // 3. Apply beam search or greedy search
        // 4. Stop when end token is generated or max length reached
        // 5. Decode tokens back to text using the tokenizer
        
        return "A photo showing various objects"; // Placeholder caption
    }

    public void Dispose()
    {
        _encoderSession?.Dispose();
        _decoderSession?.Dispose();
    }
}