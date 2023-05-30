// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Files.Backend.Services;

namespace Files.App.Actions
{
	internal class LaunchPreviewPopupAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IPreviewPopupService previewPopupService;

		public HotKey HotKey { get; } = new(Keys.Space);

		public bool IsExecutable => context.SelectedItems.Count == 1 &&
			(!context.ShellPage?.ToolbarViewModel?.IsEditModeEnabled ?? false) &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public string Label => "LaunchPreviewPopup".GetLocalizedResource();

		public string Description => "LaunchPreviewPopupDescription".GetLocalizedResource();

		public LaunchPreviewPopupAction()
		{
			previewPopupService = Ioc.Default.GetRequiredService<IPreviewPopupService>();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var provider = await previewPopupService.GetProviderAsync();
			if (provider is null)
				return;

			await provider.TogglePreviewPopup(context.SelectedItem!.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					var _ = SwitchPopupPreview();
					break;
			}
		}

		private async Task SwitchPopupPreview()
		{
			if (IsExecutable)
			{
				var provider = await previewPopupService.GetProviderAsync();
				if (provider is null)
					return;

				await provider.SwitchPreview(context.SelectedItem!.ItemPath);
			}
		}
	}
}
