using System;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Telepathic.PageModels;

namespace Telepathic.Pages;

public partial class VoicePage : ContentPage
{
    private readonly Random _random = new Random();
    private bool _isAnimating = false;
    private VoicePageModel _model;
    
    public VoicePage(VoicePageModel model)
    {
        InitializeComponent();
        BindingContext = model;
        _model = model;
        
        // Subscribe to property changed event to detect recording state changes
        _model.PropertyChanged += Model_PropertyChanged;
    }
    
    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VoicePageModel.IsRecording))
        {
            if (_model.IsRecording)
            {
                StartRippleAnimations();
            }
            else
            {
                StopRippleAnimations();
            }
        }
    }
    
    private void StartRippleAnimations()
    {
        _isAnimating = true;
        AnimateRippleAsync(Ripple1, 0).FireAndForgetSafeAsync();
        AnimateRippleAsync(Ripple2, 500).FireAndForgetSafeAsync();
        AnimateRippleAsync(Ripple3, 1000).FireAndForgetSafeAsync();
    }
    
    private void StopRippleAnimations()
    {
        _isAnimating = false;
        Ripple1.Opacity = 0;
        Ripple2.Opacity = 0;
        Ripple3.Opacity = 0;
    }
    
    private async Task AnimateRippleAsync(Ellipse ripple, int initialDelay)
    {
        await Task.Delay(initialDelay);
        
        while (_isAnimating)
        {
            // Randomize the animation a bit to simulate voice activity
            double intensity = 0.5 + (_random.NextDouble() * 0.5); // 0.5-1.0 range for scale
            uint duration = (uint)(800 + (_random.Next(400))); // 800-1200ms duration
            
            // Reset the ripple state
            ripple.Opacity = 0.8;
            ripple.Scale = 1.0;
            
            // Create animations
            var fadeAnimation = new Animation(v => ripple.Opacity = v, 0.8, 0);
            var scaleAnimation = new Animation(v => ripple.Scale = v, 1.0, 1.0 + intensity);
            
            // Create combined animation
            var parentAnimation = new Animation();
            parentAnimation.Add(0, 1, fadeAnimation);
            parentAnimation.Add(0, 1, scaleAnimation);
            
            // Start animation
            parentAnimation.Commit(this, "RippleAnimation_" + ripple.Id, 16, duration, Easing.SinOut);
            
            // Wait for animation to complete with a small random delay
            await Task.Delay((int)duration + _random.Next(200, 400));
            
            // If we're no longer animating, break the loop
            if (!_isAnimating)
                break;
        }
    }
}