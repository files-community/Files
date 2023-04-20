using CommunityToolkit.WinUI.Helpers;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Files.App.ViewModels.Settings
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

		private string _ThirdPartyNotices;
		public string ThirdPartyNotices
		{
			get => _ThirdPartyNotices;
			set => SetProperty(ref _ThirdPartyNotices, value);
		}

		public AboutViewModel()
		{
			CopyAppVersionCommand = new RelayCommand(CopyAppVersion);
			CopyWindowsVersionCommand = new RelayCommand(CopyWindowsVersion);
			SupportUsCommand = new AsyncRelayCommand(SupportUs);

			OpenDocumentationCommand = new AsyncRelayCommand(DoOpenDocumentation);
			SubmitFeatureRequestCommand = new AsyncRelayCommand(DoSubmitFeatureRequest);
			SubmitBugReportCommand = new AsyncRelayCommand(DoSubmitBugReport);

			OpenGitHubRepoCommand = new AsyncRelayCommand(DoOpenGitHubRepo);

			OpenPrivacyPolicyCommand = new AsyncRelayCommand(DoOpenPrivacyPolicy);

			OpenLogLocationCommand = new AsyncRelayCommand(OpenLogLocation);
		}

		private async Task OpenLogLocation()
		{
			await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask();
		}

		public async Task DoOpenDocumentation()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.DocumentationUrl));
		}

		public async Task DoSubmitFeatureRequest()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.FeatureRequestUrl));
		}

		public async Task DoSubmitBugReport()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.BugReportUrl));
		}

		public async Task DoOpenGitHubRepo()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.GitHubRepoUrl));
		}

		public async Task DoOpenPrivacyPolicy()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.PrivacyPolicyUrl));
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

		public async Task SupportUs()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.SupportUsUrl));
		}

		public async Task LoadThirdPartyNotices()
		{
			StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///NOTICE.md"));
			ThirdPartyNotices = await FileIO.ReadTextAsync(file);
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
