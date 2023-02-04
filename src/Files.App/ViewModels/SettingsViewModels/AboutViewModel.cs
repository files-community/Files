using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Extensions;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class AboutViewModel : ObservableObject
	{
		protected readonly IFileTagsSettingsService FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		public ICommand CopyVersionInfoCommand { get; }
		public ICommand SupportUsCommand { get; }
		public ICommand OpenLogLocationCommand { get; }
		public ICommand OpenDocumentationCommand { get; }
		public ICommand SubmitFeatureRequestCommand { get; }
		public ICommand SubmitBugReportCommand { get; }
		public ICommand OpenGitHubRepoCommand { get; }
		public ICommand OpenPrivacyPolicyCommand { get; }


		public AboutViewModel()
		{
			CopyVersionInfoCommand = new RelayCommand(CopyVersionInfo);
			SupportUsCommand = new RelayCommand(SupportUs);
			OpenDocumentationCommand = new RelayCommand(DoOpenDocumentation);
			SubmitFeatureRequestCommand = new RelayCommand(DoSubmitFeatureRequest);
			SubmitBugReportCommand = new RelayCommand(DoSubmitBugReport);
			OpenGitHubRepoCommand = new RelayCommand(DoOpenGitHubRepo);
			OpenPrivacyPolicyCommand = new RelayCommand(DoOpenPrivacyPolicy);
			OpenLogLocationCommand = new AsyncRelayCommand(OpenLogLocation);
		}

		private async Task OpenLogLocation()
		{
			await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask();
		}

		public async void DoOpenDocumentation()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.DocumentationUrl));
		}

		public async void DoSubmitFeatureRequest()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.FeatureRequestUrl));
		}

		public async void DoSubmitBugReport()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.BugReportUrl));
		}

		public async void DoOpenGitHubRepo()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.GitHubRepoUrl));
		}

		public async void DoOpenPrivacyPolicy()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.PrivacyPolicyUrl));
		}

		public void CopyVersionInfo()
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(Version + "\nOS Version: " + SystemInformation.Instance.OperatingSystemVersion);
				Clipboard.SetContent(dataPackage);
			});
		}

		public async void SupportUs()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.SupportUsUrl));
		}

		public string Version
		{
			get
			{
				var version = Package.Current.Id.Version;
				return string.Format($"{"SettingsAboutVersionTitle".GetLocalizedResource()} {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
			}
		}

		public string AppName => Package.Current.DisplayName;
	}
}
