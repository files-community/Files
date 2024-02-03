// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class LaunchPreviewPopupAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IPreviewPopupService PreviewPopupService { get; } = Ioc.Default.GetRequiredService<IPreviewPopupService>();

		public string Label
			=> "LaunchPreviewPopup".GetLocalizedResource();

		public string Description
			=> "LaunchPreviewPopupDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Space);

		public bool IsExecutable =>
			ContentPageContext.SelectedItems.Count == 1 &&
			(!ContentPageContext.ShellPage?.ToolbarViewModel?.IsEditModeEnabled ?? false) &&
			(!ContentPageContext.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public LaunchPreviewPopupAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var provider = await PreviewPopupService.GetProviderAsync();
			if (provider is null)
				return;

			var itemPath = ContentPageContext.SelectedItem?.ItemPath;
			if (itemPath is not null)
				await provider.TogglePreviewPopupAsync(itemPath);
		}

		private async Task SwitchPopupPreviewAsync()
		{
			if (IsExecutable)
			{
				var provider = await PreviewPopupService.GetProviderAsync();
				if (provider is null)
					return;

				var itemPath = ContentPageContext.SelectedItem?.ItemPath;
				if (itemPath is not null)
					await provider.SwitchPreviewAsync(itemPath);
			}
		}

		public async void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					await SwitchPopupPreviewAsync();
					break;
			}
		}
	}
}
