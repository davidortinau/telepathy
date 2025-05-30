using Telepathic.PageModels;
using Microsoft.Maui.Controls.Shapes;

namespace Telepathic.Pages;

public partial class PhotoPage : ContentPage
{
    private bool _isAnimating = false;
    private PhotoPageModel _model;
    private CancellationTokenSource? _animationCancellationTokenSource;
    
    public PhotoPage(PhotoPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
        _model = model;
        
        // Subscribe to property changed event to detect phase changes
        _model.PropertyChanged += Model_PropertyChanged;
    }

    private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PhotoPageModel.Phase))
        {
            if (_model.Phase == PhotoPhase.Analyzing)
            {
                StartScannerAnimation();
            }
            else
            {
                StopScannerAnimation();
            }
        }
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Start the scanner animation
        if (_model.Phase == PhotoPhase.Analyzing)
        {
            StartScannerAnimation();
        }
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopScannerAnimation();
    }
    
    private void StartScannerAnimation()
    {
        if (_isAnimating)
            return;
            
        _isAnimating = true;
        
        // Cancel any existing animation
        _animationCancellationTokenSource?.Cancel();
        _animationCancellationTokenSource = new CancellationTokenSource();
        
        // Start animation with proper error handling and cancellation support
        _ = AnimateScannerSafeAsync(_animationCancellationTokenSource.Token);
    }
    
    private void StopScannerAnimation()
    {
        _isAnimating = false;
        _animationCancellationTokenSource?.Cancel();
    }
    
    /// <summary>
    /// Safely start the scanner animation with proper error handling
    /// </summary>
    private async Task AnimateScannerSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await AnimateScannerAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested - no action needed
        }
        catch (Exception ex)
        {
            // Log the error but don't crash the UI
            System.Diagnostics.Debug.WriteLine($"Scanner animation error: {ex.Message}");
            // Could also use a proper logger if available in the page
        }
    }
    
    private async Task AnimateScannerAsync(CancellationToken cancellationToken = default)
    {
        // Get the height of the container
        double containerHeight = ScannerAnimation.Height;
        
        while (_isAnimating && !cancellationToken.IsCancellationRequested)
        {
            // Animate the scanner line moving from top to bottom
            await ScannerLine.TranslateTo(0, ScannerAnimation.Height - ScannerLine.Height, 2000, Easing.Linear);
            
            if (!_isAnimating || cancellationToken.IsCancellationRequested) break;
            
            // Animate the scanner line moving from bottom to top
            await ScannerLine.TranslateTo(0, 0, 2000, Easing.Linear);
            
            if (!_isAnimating || cancellationToken.IsCancellationRequested) break;
        }
        
        // Reset position when stopped
        await ScannerLine.TranslateTo(0, 0, 0);
    }
}
