using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Extensions;
using Files.Core.Services.Settings;
using Files.Core.Extensions;
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

		public ICommand CopyAppVersionCommand { get; }
		public ICommand CopyWindowsVersionCommand { get; }
		public ICommand SupportUsCommand { get; }
		public ICommand OpenLogLocationCommand { get; }
		public ICommand OpenDocumentationCommand { get; }
		public ICommand SubmitFeatureRequestCommand { get; }
		public ICommand SubmitBugReportCommand { get; }
		public ICommand OpenGitHubRepoCommand { get; }
		public ICommand OpenPrivacyPolicyCommand { get; }


		public AboutViewModel()
		{
			CopyAppVersionCommand = new RelayCommand(CopyAppVersion);
			CopyWindowsVersionCommand = new RelayCommand(CopyWindowsVersion);
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
			await Launcher.LaunchUriAsync(new Uri(Core.Constants.GitHub.DocumentationUrl));
		}

		public async void DoSubmitFeatureRequest()
		{
			await Launcher.LaunchUriAsync(new Uri(Core.Constants.GitHub.FeatureRequestUrl));
		}

		public async void DoSubmitBugReport()
		{
			await Launcher.LaunchUriAsync(new Uri(Core.Constants.GitHub.BugReportUrl));
		}

		public async void DoOpenGitHubRepo()
		{
			await Launcher.LaunchUriAsync(new Uri(Core.Constants.GitHub.GitHubRepoUrl));
		}

		public async void DoOpenPrivacyPolicy()
		{
			await Launcher.LaunchUriAsync(new Uri(Core.Constants.GitHub.PrivacyPolicyUrl));
		}

		public void CopyAppVersion()
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(string.Format($"{AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}.{AppVersion.Revision}"));
				Clipboard.SetContent(dataPackage);
			});
		}
		
		public void CopyWindowsVersion()
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(SystemInformation.Instance.OperatingSystemVersion.ToString());
				Clipboard.SetContent(dataPackage);
			});
		}

		public async void SupportUs()
		{
			await Launcher.LaunchUriAsync(new Uri(Core.Constants.GitHub.SupportUsUrl));
		}

		public string Version
		{
			get
			{
				return string.Format($"{"SettingsAboutVersionTitle".GetLocalizedResource()} {AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}.{AppVersion.Revision}");
			}
		}

		public string AppName => Package.Current.DisplayName;
		public PackageVersion AppVersion => Package.Current.Id.Version;
	}
}
