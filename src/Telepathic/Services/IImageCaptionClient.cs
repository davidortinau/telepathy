namespace Telepathic.Services;

/// <summary>
/// Interface for image captioning services
/// </summary>
public interface IImageCaptionClient
{
    /// <summary>
    /// Generate a caption for the provided image
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated caption text</returns>
    Task<string> GenerateCaptionAsync(string imagePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate a caption for the provided image stream
    /// </summary>
    /// <param name="imageStream">Image stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated caption text</returns>
    Task<string> GenerateCaptionAsync(Stream imageStream, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the client is available for use
    /// </summary>
    bool IsAvailable { get; }
}