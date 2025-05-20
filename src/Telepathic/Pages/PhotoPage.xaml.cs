using Telepathic.PageModels;
using Microsoft.Maui.Controls.Shapes;

namespace Telepathic.Pages;

public partial class PhotoPage : ContentPage
{
    private bool _isAnimating = false;
    private PhotoPageModel _model;
    
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
        AnimateScanner();
    }
    
    private void StopScannerAnimation()
    {
        _isAnimating = false;
    }
    
    private async void AnimateScanner()
    {
        // Get the height of the container
        double containerHeight = ScannerAnimation.Height;
        
        while (_isAnimating)
        {
            // Animate the scanner line moving from top to bottom
            await ScannerLine.TranslateToAsync(0, containerHeight - ScannerLine.Height, 2000, Easing.Linear);
            
            if (!_isAnimating) break;
            
            // Animate the scanner line moving from bottom to top
            await ScannerLine.TranslateToAsync(0, 0, 2000, Easing.Linear);
            
            if (!_isAnimating) break;
        }
        
        // Reset position when stopped
        await ScannerLine.TranslateToAsync(0, 0, 0);
    }
}
