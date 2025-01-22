// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public sealed partial class DevToolsViewModel : ObservableObject
	{
		protected readonly IFileTagsSettingsService FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		protected readonly IDevToolsSettingsService DevToolsSettingsService = Ioc.Default.GetRequiredService<IDevToolsSettingsService>();

		public Dictionary<OpenInIDEOption, string> OpenInIDEOptions { get; private set; } = [];
		public ICommand RemoveCredentialsCommand { get; }
		public ICommand ConnectToGitHubCommand { get; }
		public ICommand StartEditingIDECommand { get; }
		public ICommand CancelIDEChangesCommand { get; }
		public ICommand SaveIDEChangesCommand { get; }
		public ICommand OpenFilePickerForIDECommand { get; }

		// Enabled when there are saved credentials
		private bool _IsLogoutEnabled;
		public bool IsLogoutEnabled
		{
			get => _IsLogoutEnabled;
			set => SetProperty(ref _IsLogoutEnabled, value);
		}

		private bool _IsEditingIDEConfig;
		public bool IsEditingIDEConfig
		{
			get => _IsEditingIDEConfig;
			set => SetProperty(ref _IsEditingIDEConfig, value);
		}

		private bool _CanSaveIDEChanges;
		public bool CanSaveIDEChanges
		{
			get => _CanSaveIDEChanges;
			set => SetProperty(ref  _CanSaveIDEChanges, value);
		}

		private string _IDEPath;
		public string IDEPath
		{
			get => _IDEPath;
			set => SetProperty(ref _IDEPath, value);
		}

		private string _IDEFriendlyName;
		public string IDEFriendlyName
		{
			get => _IDEFriendlyName;
			set => SetProperty(ref _IDEFriendlyName, value);
		}

		public DevToolsViewModel()
		{
			// Open in IDE options
			OpenInIDEOptions.Add(OpenInIDEOption.GitRepos, "GitRepos".GetLocalizedResource());
			OpenInIDEOptions.Add(OpenInIDEOption.AllLocations, "AllLocations".GetLocalizedResource());
			SelectedOpenInIDEOption = OpenInIDEOptions[DevToolsSettingsService.OpenInIDEOption];

			IDEPath = DevToolsSettingsService.IDEPath;
			IDEFriendlyName = DevToolsSettingsService.FriendlyIDEName;

			IsLogoutEnabled = GitHelpers.GetSavedCredentials() != string.Empty;

			RemoveCredentialsCommand = new RelayCommand(DoRemoveCredentials);
			ConnectToGitHubCommand = new RelayCommand(DoConnectToGitHubAsync);
			CancelIDEChangesCommand = new RelayCommand(DoCancelIDEChanges);
			SaveIDEChangesCommand = new RelayCommand(DoSaveIDEChanges);
			StartEditingIDECommand = new RelayCommand(DoStartEditingIDE);
			OpenFilePickerForIDECommand = new RelayCommand(DoOpenFilePickerForIDE);
		}

		private string selectedOpenInIDEOption;
		public string SelectedOpenInIDEOption
		{
			get => selectedOpenInIDEOption;
			set
			{
				if (SetProperty(ref selectedOpenInIDEOption, value))
				{
					DevToolsSettingsService.OpenInIDEOption = OpenInIDEOptions.First(e => e.Value == value).Key;
				}
			}
		}

		public void DoRemoveCredentials()
		{
			GitHelpers.RemoveSavedCredentials();
			IsLogoutEnabled = false;
		}

		public async void DoConnectToGitHubAsync()
		{
			UIHelpers.CloseAllDialogs();

			await Task.Delay(500);

			await GitHelpers.RequireGitAuthenticationAsync();
		}

		private void DoCancelIDEChanges()
		{
			IsEditingIDEConfig = false;
			IDEPath = DevToolsSettingsService.IDEPath;
			IDEFriendlyName = DevToolsSettingsService.FriendlyIDEName;
		}

		private void DoSaveIDEChanges()
		{
			IsEditingIDEConfig = false;
			DevToolsSettingsService.IDEPath = IDEPath;
			DevToolsSettingsService.FriendlyIDEName = IDEFriendlyName;
		}

		private void DoStartEditingIDE()
		{
			IsEditingIDEConfig = true;
		}

		private void DoOpenFilePickerForIDE()
		{

		}
	}
}
