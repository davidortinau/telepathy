using Telepathic.Models;
using Telepathic.PageModels;
using System.Windows.Input;
using CommunityToolkit.Maui;

namespace Telepathic.Pages;

public partial class MainPage : ContentPage
{
	private bool _isActionButtonsExpanded = false;
	
	public MainPage(MainPageModel model)
	{
		Console.WriteLine("BEEP");
		InitializeComponent();
		BindingContext = model;
		
		// Initialize the action buttons to be hidden
		SetupInitialButtonStates();
	}
	
	private void SetupInitialButtonStates()
	{
		// Initially hide the buttons
		CameraButton.Opacity = 0;
		MicrophoneButton.Opacity = 0;
		KeyboardButton.Opacity = 0;
		
		// Position them behind the add button
		CameraButton.TranslationY = 180;
		MicrophoneButton.TranslationY = 120;
		KeyboardButton.TranslationY = 60;
	}
		private void ToggleActionButtons_Clicked(object sender, EventArgs e)
	{
		// Check if telepathy is enabled
		if (BindingContext is MainPageModel model && model.IsTelepathyEnabled)
		{
			// Toggle the expanded state
			_isActionButtonsExpanded = !_isActionButtonsExpanded;
			
			// Animate based on the new state
			if (_isActionButtonsExpanded)
			{
				ShowActionButtons();
			}
			else
			{
				HideActionButtons();
			}
		}
		else
		{
			// If telepathy is not enabled, just execute the AddTaskCommand
			if (BindingContext is MainPageModel viewModel && viewModel.AddTaskCommand != null)
			{
				viewModel.AddTaskCommand.Execute(null);
			}
		}
	}
	private void ShowActionButtons()
	{
		// Animate the buttons to appear
		Animation cameraAnimation = new Animation(
			callback: v => { CameraButton.Opacity = v; CameraButton.TranslationY = 180 - (180 * v); },
			start: 0,
			end: 1,
			easing: Easing.SpringOut);
			
		Animation microphoneAnimation = new Animation(
			callback: v => { MicrophoneButton.Opacity = v; MicrophoneButton.TranslationY = 120 - (120 * v); },
			start: 0,
			end: 1,
			easing: Easing.SpringOut);
			
		Animation keyboardAnimation = new Animation(
			callback: v => { KeyboardButton.Opacity = v; KeyboardButton.TranslationY = 60 - (60 * v); },
			start: 0,
			end: 1,
			easing: Easing.SpringOut);
		
		// Store the original color for later restoration
		Color originalColor = ActionToggleButton.BackgroundColor;
		
		// Rotate the add button to look like X (45 degrees)
		Animation addButtonRotation = new Animation(
			callback: v => ActionToggleButton.Rotation = v,
			start: 0,
			end: 45,
			easing: Easing.CubicOut);
		
		// Change the background color to black
		Animation addButtonColor = new Animation(
			callback: v => ActionToggleButton.BackgroundColor = Color.FromRgba(
				originalColor.Red * (1 - v),
				originalColor.Green * (1 - v),
				originalColor.Blue * (1 - v),
				1),
			start: 0,
			end: 1,
			easing: Easing.CubicOut);
		
		// Start animations with slight delay between each
		cameraAnimation.Commit(this, "CameraAnimation", 16, 300, Easing.SpringOut);
		microphoneAnimation.Commit(this, "MicrophoneAnimation", 16, 300, Easing.SpringOut);
		keyboardAnimation.Commit(this, "KeyboardAnimation", 16, 300, Easing.SpringOut);
		addButtonRotation.Commit(this, "AddButtonRotationAnimation", 16, 250, Easing.CubicOut);
		addButtonColor.Commit(this, "AddButtonColorAnimation", 16, 250, Easing.CubicOut);
	}
	
	private void HideActionButtons()
	{
		// Animate the buttons to disappear
		Animation cameraAnimation = new Animation(
			callback: v => { CameraButton.Opacity = 1 - v; CameraButton.TranslationY = v * 180; },
			start: 0,
			end: 1,
			easing: Easing.SpringIn);
			
		Animation microphoneAnimation = new Animation(
			callback: v => { MicrophoneButton.Opacity = 1 - v; MicrophoneButton.TranslationY = v * 120; },
			start: 0,
			end: 1,
			easing: Easing.SpringIn);
			
		Animation keyboardAnimation = new Animation(
			callback: v => { KeyboardButton.Opacity = 1 - v; KeyboardButton.TranslationY = v * 60; },
			start: 0,
			end: 1,
			easing: Easing.SpringIn);
		
		// Get the primary color from resources - safely
		Color primaryColor;
		if (Application.Current != null &&
			Application.Current.Resources.TryGetValue("Primary", out object colorValue) &&
			colorValue is AppThemeColor color)
		{
			primaryColor = color.Dark!;
		}
		else
		{
			// Fallback color if we can't get the primary color
			primaryColor = Colors.Blue;
		}
		
		// Rotate the add button back to original position
		Animation addButtonRotation = new Animation(
			callback: v => ActionToggleButton.Rotation = 45 - (v * 45),
			start: 0,
			end: 1,
			easing: Easing.CubicIn);
		
		// Change the background color back to primary
		Animation addButtonColor = new Animation(
			callback: v => ActionToggleButton.BackgroundColor = Color.FromRgba(
				v * primaryColor.Red,
				v * primaryColor.Green, 
				v * primaryColor.Blue,
				1),
			start: 0,
			end: 1,
			easing: Easing.CubicIn);
		
		// Start animations with slight delay between each
		keyboardAnimation.Commit(this, "KeyboardAnimation", 16, 250, Easing.SpringIn);
		microphoneAnimation.Commit(this, "MicrophoneAnimation", 16, 250, Easing.SpringIn);
		cameraAnimation.Commit(this, "CameraAnimation", 16, 250, Easing.SpringIn);
		addButtonRotation.Commit(this, "AddButtonRotationAnimation", 16, 250, Easing.CubicIn);
		addButtonColor.Commit(this, "AddButtonColorAnimation", 16, 250, Easing.CubicIn);
	}
	
	private void CalendarCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		if (sender is CheckBox checkBox && checkBox.BindingContext is CalendarInfo calendar && BindingContext is MainPageModel viewModel)
		{
			viewModel.ToggleCalendarSelectionCommand.Execute(calendar);
		}
	}
}