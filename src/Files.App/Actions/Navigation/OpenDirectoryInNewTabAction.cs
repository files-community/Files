// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenDirectoryInNewTabAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly IUserSettingsService userSettingsService;

		public string Label
			=> "OpenInNewTab".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewTabDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenInNewTab");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.ShellPage.SlimContentPage is not null &&
			context.SelectedItems.Count <= 5 &&
			context.SelectedItems.Where(x => x.IsFolder == true).Count() == context.SelectedItems.Count &&
			userSettingsService.GeneralSettingsService.ShowOpenInNewTab;

		public OpenDirectoryInNewTabAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage?.SlimContentPage?.SelectedItems is null)
				return;

			foreach (ListedItem listedItem in context.ShellPage.SlimContentPage.SelectedItems)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await NavigationHelpers.AddNewTabByPathAsync(
						typeof(PaneHolderPage),
						(listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
				},
				Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
