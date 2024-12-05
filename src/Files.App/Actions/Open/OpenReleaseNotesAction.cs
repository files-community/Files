// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;

namespace Files.App.Actions
{
	internal sealed class OpenReleaseNotesAction : ObservableObject, IAction
	{
		private readonly IDialogService DialogService = Ioc.Default.GetRequiredService<IDialogService>();
		private readonly IUpdateService UpdateService = Ioc.Default.GetRequiredService<IUpdateService>();
		public string Label
			=> Strings.WhatsNew.GetLocalizedResource();

		public string Description
			=> Strings.WhatsNewDescription.GetLocalizedResource();

		public bool IsExecutable
			=> UpdateService.AreReleaseNotesAvailable;

		public OpenReleaseNotesAction()
		{
			UpdateService.PropertyChanged += UpdateService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var applicationVersion = $"v{SystemInformation.Instance.ApplicationVersion.Major}-{SystemInformation.Instance.ApplicationVersion.Minor}-{SystemInformation.Instance.ApplicationVersion.Build}";
			var releaseNotesLocation = $"https://5ee883e8.files-website-cml.pages.dev/blog/posts/{applicationVersion}?minimal=true";

			var viewModel = new ReleaseNotesDialogViewModel(releaseNotesLocation);
			var dialog = DialogService.GetDialog(viewModel);

			return dialog.TryShowAsync();
		}

		private void UpdateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IUpdateService.AreReleaseNotesAvailable):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
