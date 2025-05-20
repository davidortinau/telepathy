using CommunityToolkit.Maui;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using Plugin.Maui.Audio;
using Plugin.Maui.CalendarStore;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using Telepathic.Shared.Services;
using Syncfusion.Blazor;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Telepathic;

public static class MauiProgram
{   
	public static MauiApp CreateMauiApp()
	{
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Shared.Utilities.SFKey.GetSFKeyValue());
        
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.ConfigureSyncfusionToolkit()
			.ConfigureMauiHandlers(handlers =>
			{
			})
			.AddAudio(
				playbackOptions =>
				{
#if IOS || MACCATALYST
					playbackOptions.Category = AVFoundation.AVAudioSessionCategory.Playback;
#endif
#if ANDROID
					// playbackOptions.AudioContentType = Android.Media.AudioContentType.Music;
					// playbackOptions.AudioUsageKind = Android.Media.AudioUsageKind.Media;
#endif
				},
				recordingOptions =>
				{
#if IOS || MACCATALYST
					recordingOptions.Category = AVFoundation.AVAudioSessionCategory.Record;
					recordingOptions.Mode = AVFoundation.AVAudioSessionMode.Default;
					recordingOptions.CategoryOptions = AVFoundation.AVAudioSessionCategoryOptions.MixWithOthers;
#endif
				})
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
				fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
			});

		builder.AddServiceDefaults();

        builder.Services.AddMauiBlazorWebView();
		builder.Services.AddSyncfusionBlazor();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
		builder.Services.AddLogging(configure => configure.AddDebug());
#endif        

        builder.Services.AddSingleton(CalendarStore.Default);
		builder.Services.AddSingleton<ProjectRepository>();
		builder.Services.AddSingleton<TaskRepository>();
		builder.Services.AddSingleton<CategoryRepository>();
		builder.Services.AddSingleton<TagRepository>();
		builder.Services.AddSingleton<SeedDataService>();
		builder.Services.AddSingleton<ModalErrorHandler>();
		builder.Services.AddSingleton<MainPageModel>();
		builder.Services.AddSingleton<ProjectListPageModel>();
		builder.Services.AddSingleton<ManageMetaPageModel>();
		builder.Services.AddSingleton<IAudioService, AudioService>();
		builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
		builder.Services.AddSingleton<IChatClientService, ChatClientService>();		
		builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
		builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");
		builder.Services.AddTransientWithShellRoute<Pages.VoiceModalPage, PageModels.VoiceModalPageModel>("voice");
		builder.Services.AddTransientWithShellRoute<Pages.PhotoPage, PageModels.PhotoPageModel>("photo");

        builder.Services.AddSingleton<IFormFactor, FormFactor>();
		builder.Services.AddSingleton<ITeamDataService, TeamDataService>();

        return builder.Build();
	}
}
