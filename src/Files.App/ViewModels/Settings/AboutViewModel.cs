// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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

		private Task OpenLogLocation()
		{
			return Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask();
		}

		public Task DoOpenDocumentation()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.GitHub.DocumentationUrl)).AsTask();
		}

		public Task DoSubmitFeatureRequest()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.GitHub.FeatureRequestUrl)).AsTask();
		}

		public Task DoSubmitBugReport()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.GitHub.BugReportUrl)).AsTask();
		}

		public Task DoOpenGitHubRepo()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.GitHub.GitHubRepoUrl)).AsTask();
		}

		public Task DoOpenPrivacyPolicy()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.GitHub.PrivacyPolicyUrl)).AsTask();
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

		public Task SupportUs()
        {
            return Launcher.LaunchUriAsync(new Uri(Constants.GitHub.SupportUsUrl)).AsTask();
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
