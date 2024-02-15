// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenDirectoryInNewTabAction : ObservableObject, IAction
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		private ActionExecutableType ExecutableType { get; set; }

		public string Label
			=> "OpenInNewTab".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewTabDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenInNewTab");

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenDirectoryInNewTabAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			switch (ExecutableType)
			{
				case ActionExecutableType.DisplayPageContext:
					{
						foreach (ListedItem listedItem in ContentPageContext.ShellPage?.SlimContentPage.SelectedItems!)
						{
							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								await NavigationHelpers.AddNewTabByPathAsync(
									typeof(PaneHolderPage),
									(listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
							},
							Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
						}
						break;
					}
				case ActionExecutableType.HomePageContext:
					{
						await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							await NavigationHelpers.AddNewTabByPathAsync(
								typeof(PaneHolderPage),
								HomePageContext.RightClickedItem?.Path ?? string.Empty);
						},
						Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
						break;
					}
			}
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.ShellPage is not null &&
				ContentPageContext.ShellPage.SlimContentPage is not null &&
				ContentPageContext.HasSelection &&
				ContentPageContext.SelectedItems.Count <= 5 &&
				ContentPageContext.SelectedItems.Where(x => x.IsFolder == true).Count() == ContentPageContext.SelectedItems.Count &&
				UserSettingsService.GeneralSettingsService.ShowOpenInNewTab;

			if (executableInDisplayPage)
				ExecutableType = ActionExecutableType.DisplayPageContext;

			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked;

			if (executableInHomePage)
				ExecutableType = ActionExecutableType.HomePageContext;

			return executableInDisplayPage || executableInHomePage;
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
