# ONNX Model Setup Guide

This document explains how to set up the ONNX models for local image captioning and voice transcription in the Telepathic app.

## Overview

The app supports local inference capabilities using ONNX models for:
- **Image Captioning**: Using ViT-GPT2 model for describing images
- **Voice Transcription**: Using Whisper-tiny model for speech-to-text

## Model Files Required

### Image Captioning (ViT-GPT2)
Download from: https://huggingface.co/Xenova/vit-gpt2-image-captioning/tree/main

Required files:
- `encoder_model.onnx` (Vision Transformer encoder)
- `decoder_model_merged.onnx` (GPT-2 decoder)
- `tokenizer.json` (GPT-2 tokenizer configuration)

Place these files in: `src/Telepathic/Resources/Raw/Models/ImageCaptioning/`

### Voice Transcription (Whisper-tiny)
Download from: https://huggingface.co/openai/whisper-tiny/tree/main

Required files:
- `encoder_model.onnx` (Whisper audio encoder)
- `decoder_model_merged.onnx` (Whisper text decoder)
- `tokenizer.json` (Whisper tokenizer configuration)

Place these files in: `src/Telepathic/Resources/Raw/Models/VoiceTranscription/`

## Manual Download Instructions

Since the build environment doesn't have internet access, you'll need to download these manually:

### Step 1: Download Image Captioning Models
```bash
curl -L -o encoder_model.onnx https://huggingface.co/Xenova/vit-gpt2-image-captioning/resolve/main/encoder_model.onnx
curl -L -o decoder_model_merged.onnx https://huggingface.co/Xenova/vit-gpt2-image-captioning/resolve/main/decoder_model_merged.onnx
curl -L -o tokenizer.json https://huggingface.co/Xenova/vit-gpt2-image-captioning/resolve/main/tokenizer.json
```

### Step 2: Download Voice Transcription Models
```bash
curl -L -o encoder_model.onnx https://huggingface.co/openai/whisper-tiny/resolve/main/encoder_model.onnx
curl -L -o decoder_model_merged.onnx https://huggingface.co/openai/whisper-tiny/resolve/main/decoder_model_merged.onnx
curl -L -o tokenizer.json https://huggingface.co/openai/whisper-tiny/resolve/main/tokenizer.json
```

### Step 3: Copy to Project
Copy the downloaded files to their respective directories in the project structure.

## Build Configuration

The project is already configured to include these models as content files:
- Models are included as `MauiAsset` and `Content` items
- They will be deployed with the app package
- Models are accessible via `FileSystem.OpenAppPackageFileAsync()`

## Hybrid Workflow

The app implements a hybrid approach:

1. **Local First**: If ONNX models are available and initialized, use local inference
2. **Cloud Fallback**: If local models fail or are unavailable, fallback to OpenAI APIs
3. **Graceful Degradation**: App continues to work even without local models

## Implementation Details

### Services Created:
- `IImageCaptionClient` + `OnnxImageCaptionClient`: Local image captioning
- `ILocalTranscriptionService` + `OnnxVoiceTranscriptionClient`: Local voice transcription  
- `HybridTranscriptionService`: Hybrid transcription (local first, OpenAI fallback)

### Integration Points:
- `ChatClientService.GetResponseWithImageAsync()`: Uses local image captioning to enhance prompts
- `VoicePageModel`: Uses hybrid transcription service automatically
- `PhotoPageModel`: Uses enhanced image analysis via ChatClientService

## Testing Without Models

The app will function normally without ONNX models installed:
- Local services will report `IsAvailable = false`
- Hybrid services automatically fallback to OpenAI APIs
- No crashes or errors occur

## File Sizes (Approximate)

**Image Captioning Models:**
- encoder_model.onnx: ~90MB
- decoder_model_merged.onnx: ~550MB
- tokenizer.json: ~2MB

**Voice Transcription Models:**
- encoder_model.onnx: ~15MB
- decoder_model_merged.onnx: ~25MB
- tokenizer.json: ~500KB

**Total**: ~680MB for both model sets