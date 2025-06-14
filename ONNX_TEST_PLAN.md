# ONNX Integration Test Plan

This document outlines how to test the ONNX integration once the models are properly installed.

## Test Scenarios

### 1. Without ONNX Models (Fallback Testing)
**Expected Behavior**: App should work normally using OpenAI APIs

**Test Steps**:
1. Ensure no real ONNX model files are present (only .placeholder files)
2. Launch app and test voice recording feature
3. Launch app and test photo analysis feature
4. Verify logs show "Local ONNX transcription not available, using OpenAI"
5. Verify logs show "Local ONNX image captioning not available, using original prompt"

### 2. With ONNX Models (Local Inference Testing)
**Expected Behavior**: App should prefer local ONNX models over OpenAI

**Test Steps**:
1. Download and install proper ONNX model files (see ONNX_SETUP.md)
2. Launch app and test voice recording feature
3. Launch app and test photo analysis feature
4. Verify logs show "Attempting transcription using local ONNX model..."
5. Verify logs show "Using local ONNX model for image captioning"

### 3. Error Handling Testing
**Expected Behavior**: App should gracefully fallback when local models fail

**Test Steps**:
1. Install corrupted ONNX model files
2. Test voice and photo features
3. Verify app doesn't crash and falls back to OpenAI
4. Check logs for appropriate error messages

## Service Integration Tests

### Test HybridTranscriptionService
```csharp
// Create services
var localService = new OnnxVoiceTranscriptionClient(logger);
var hybridService = new HybridTranscriptionService(localService, logger);

// Test transcription
var result = await hybridService.TranscribeAsync("test_audio.wav", CancellationToken.None);
```

### Test Image Captioning
```csharp
// Create service
var imageCaptionClient = new OnnxImageCaptionClient(logger);

// Test captioning
var caption = await imageCaptionClient.GenerateCaptionAsync("test_image.jpg");
```

### Test ChatClientService Integration
```csharp
// Create enhanced chat service
var chatService = new ChatClientService(logger, locationTools, imageCaptionClient);

// Test image-enhanced prompt
var response = await chatService.GetResponseWithImageAsync<ProjectsJson>(
    "Extract tasks from this image", 
    "image_path.jpg"
);
```

## Performance Benchmarks

### Expected Performance (approximate):
- **Local Image Captioning**: 2-5 seconds on mobile device
- **Local Voice Transcription**: 1-3 seconds for 30-second audio
- **OpenAI Fallback**: 3-10 seconds (depends on network)

### Memory Usage:
- **Image Models**: ~650MB RAM during inference
- **Voice Models**: ~40MB RAM during inference

## Validation Checklist

- [ ] App starts without ONNX models (fallback mode)
- [ ] App starts with ONNX models (local mode)
- [ ] Voice transcription works in both modes
- [ ] Photo analysis works in both modes
- [ ] Error logs are appropriate and informative
- [ ] No crashes or exceptions during normal operation
- [ ] Performance is acceptable for local inference
- [ ] Memory usage is reasonable

## Known Limitations

1. **Model Loading Time**: Initial model loading may take 5-10 seconds
2. **Model Size**: Total models are ~680MB - may impact app size significantly
3. **Platform Support**: ONNX Runtime may have platform-specific issues
4. **Placeholder Implementation**: Current ONNX implementations use simplified inference loops
5. **Tokenizer Parsing**: Tokenizer loading needs full implementation for production use

## Next Steps for Production

1. **Implement Full Tokenizer Support**: Parse tokenizer.json properly
2. **Add Beam Search**: Implement proper text generation algorithms
3. **Optimize Performance**: Add model caching and optimization
4. **Add Model Versioning**: Support different model versions
5. **Add Progress Indicators**: Show model loading and inference progress
6. **Add Model Download**: Allow downloading models from within the app