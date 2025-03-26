// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public sealed partial class DevToolsViewModel : ObservableObject
	{
		protected readonly IFileTagsSettingsService FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		protected readonly IDevToolsSettingsService DevToolsSettingsService = Ioc.Default.GetRequiredService<IDevToolsSettingsService>();
		private readonly ICommonDialogService CommonDialogService = Ioc.Default.GetRequiredService<ICommonDialogService>();

		public Dictionary<OpenInIDEOption, string> OpenInIDEOptions { get; private set; } = [];
		public ICommand RemoveCredentialsCommand { get; }
		public ICommand ConnectToGitHubCommand { get; }
		public ICommand StartEditingIDECommand { get; }
		public ICommand CancelIDEChangesCommand { get; }
		public ICommand SaveIDEChangesCommand { get; }
		public ICommand OpenFilePickerForIDECommand { get; }
		public ICommand TestIDECommand { get; }

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

		public bool CanSaveIDEChanges =>
			IsIDENameValid && IsIDEPathValid;

		private bool _IsIDEPathValid;
		public bool IsIDEPathValid
		{
			get => _IsIDEPathValid;
			set => SetProperty(ref _IsIDEPathValid, value);
		}

		private bool _IsIDENameValid;
		public bool IsIDENameValid
		{
			get => _IsIDENameValid;
			set => SetProperty(ref _IsIDENameValid, value);
		}

		private string _IDEPath;
		public string IDEPath
		{
			get => _IDEPath;
			set
			{
				if (SetProperty(ref _IDEPath, value))
				{
					IsIDEPathValid =
						!string.IsNullOrWhiteSpace(value) &&
						!value.Contains('\"') &&
						!value.Contains('\'') &&
						CheckPathExists();

					OnPropertyChanged(nameof(CanSaveIDEChanges));
				}
			}
		}

		private string _IDEName;
		public string IDEName
		{
			get => _IDEName;
			set
			{
				if (SetProperty(ref _IDEName, value))
				{
					IsIDENameValid = !string.IsNullOrEmpty(value);
					OnPropertyChanged(nameof(CanSaveIDEChanges));
				}
			}
		}

		public DevToolsViewModel()
		{
			// Open in IDE options
			OpenInIDEOptions.Add(OpenInIDEOption.GitRepos, Strings.GitRepos.GetLocalizedResource());
			OpenInIDEOptions.Add(OpenInIDEOption.AllLocations, Strings.AllLocations.GetLocalizedResource());
			SelectedOpenInIDEOption = OpenInIDEOptions[DevToolsSettingsService.OpenInIDEOption];

			IDEPath = DevToolsSettingsService.IDEPath;
			IDEName = DevToolsSettingsService.IDEName;
			IsIDEPathValid = true;
			IsIDENameValid = true;

			IsLogoutEnabled = GitHelpers.GetSavedCredentials() != string.Empty;

			RemoveCredentialsCommand = new RelayCommand(DoRemoveCredentials);
			ConnectToGitHubCommand = new RelayCommand(DoConnectToGitHubAsync);
			CancelIDEChangesCommand = new RelayCommand(DoCancelIDEChanges);
			SaveIDEChangesCommand = new RelayCommand(DoSaveIDEChanges);
			StartEditingIDECommand = new RelayCommand(DoStartEditingIDE);
			OpenFilePickerForIDECommand = new RelayCommand(DoOpenFilePickerForIDE);
			TestIDECommand = new RelayCommand(DoTestIDE);
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
			IDEName = DevToolsSettingsService.IDEName;
			IsIDEPathValid = true;
			IsIDENameValid = true;
		}

		private void DoSaveIDEChanges()
		{
			IsEditingIDEConfig = false;
			IsIDEPathValid = true;
			IsIDENameValid = true;
			DevToolsSettingsService.IDEPath = IDEPath;
			DevToolsSettingsService.IDEName = IDEName;
		}

		private void DoStartEditingIDE()
		{
			IsEditingIDEConfig = true;
		}

		private void DoOpenFilePickerForIDE()
		{
			var res = CommonDialogService.Open_FileOpenDialog(
				MainWindow.Instance.WindowHandle,
				false,
				["*.exe;*.bat;*.cmd;*.ahk"],
				Environment.SpecialFolder.ProgramFiles,
				out var filePath
			);

			if (res)
				IDEPath = filePath;
		}

		private async void DoTestIDE()
		{
			IsIDEPathValid = await Win32Helper.RunPowershellCommandAsync(
				$"& \'{IDEPath}\'",
				PowerShellExecutionOptions.Hidden
			);
		}

		private bool CheckPathExists()
		{
			if (Path.Exists(IDEPath))
				return true;

			var paths = Environment.GetEnvironmentVariable("PATH")?.Split(';');
			foreach (var path in paths ?? Array.Empty<string>())
			{
				if (Path.Exists(Path.Combine(path, IDEPath)))
					return true;
			}

			return false;
		}
	}
}
