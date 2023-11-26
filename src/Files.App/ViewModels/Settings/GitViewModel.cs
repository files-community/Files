// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public class GitViewModel : ObservableObject
	{
		protected readonly IFileTagsSettingsService FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		public ICommand RemoveCredentialsCommand { get; }
		public ICommand ConnectToGitHubCommand { get; }

		// Enabled when there are saved credentials
		private bool _IsLogoutEnabled;
		public bool IsLogoutEnabled
		{
			get => _IsLogoutEnabled;
			set => SetProperty(ref _IsLogoutEnabled, value);
		}

		public GitViewModel()
		{
			RemoveCredentialsCommand = new RelayCommand(DoRemoveCredentials);
			ConnectToGitHubCommand = new RelayCommand(DoConnectToGitHubAsync);

			IsLogoutEnabled = GitHelpers.GetSavedCredentials() != string.Empty;
		}

		public void DoRemoveCredentials()
		{
			GitHelpers.RemoveSavedCredentials();
			IsLogoutEnabled = false;
		}
		
		public async void DoConnectToGitHubAsync()
		{
			UIHelpers.CloseAllDialogs();
			await GitHelpers.RequireGitAuthenticationAsync();
		}
	}
}
