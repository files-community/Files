﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Files.Backend.Services;

namespace Files.App.Actions
{
	internal class LaunchQuickLookAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IPreviewPopupService previewPopupService;

		public HotKey HotKey { get; } = new(Keys.Space);

		public bool IsExecutable => context.SelectedItems.Count == 1 &&
			(!context.ShellPage?.ToolbarViewModel?.IsEditModeEnabled ?? false) &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public string Label => "LaunchQuickLook".GetLocalizedResource();

		public string Description => "LaunchQuickLookDescription".GetLocalizedResource();

		public LaunchQuickLookAction()
		{
			previewPopupService = Ioc.Default.GetRequiredService<IPreviewPopupService>();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await previewPopupService.TogglePreviewPopup(context.SelectedItem!.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					var _ = SwitchQuickLookPreview();
					break;
			}
		}

		private async Task SwitchQuickLookPreview()
		{
			if (IsExecutable)
				await previewPopupService.SwitchPreview(context.SelectedItem!.ItemPath);
		}
	}
}
