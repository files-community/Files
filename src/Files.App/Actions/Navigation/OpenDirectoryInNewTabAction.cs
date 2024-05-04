// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenDirectoryInNewTabAction : ObservableObject, IAction
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		private ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		private ExecutableContextType _executableContextType;

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
			switch (_executableContextType)
			{
				case ExecutableContextType.ContentPageContext:
					{
						foreach (ListedItem listedItem in ContentPageContext.SelectedItems)
						{
							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								await NavigationHelpers.AddNewTabByPathAsync(
									typeof(PaneHolderPage),
									(listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath,
									false);
							},
							Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
						}

						break;
					}
				case ExecutableContextType.HomePageContext:
					{
						await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							await NavigationHelpers.AddNewTabByPathAsync(
								typeof(PaneHolderPage),
								HomePageContext.RightClickedItem?.Path ?? string.Empty,
								false);
						},
						Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

						break;
					}
				case ExecutableContextType.SidebarContext:
					{
						await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							await NavigationHelpers.AddNewTabByPathAsync(
								typeof(PaneHolderPage),
								SidebarContext.RightClickedItem?.Path ?? string.Empty,
								false);
						},
						Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

						break;
					}
			}
		}

		private bool GetIsExecutable()
		{
			if (ContentPageContext.ShellPage is not null &&
				ContentPageContext.ShellPage.SlimContentPage is not null &&
				ContentPageContext.SelectedItems.Count is not 0 &&
				ContentPageContext.SelectedItems.Count <= 5 &&
				ContentPageContext.SelectedItems.Count(x => x.IsFolder) == ContentPageContext.SelectedItems.Count &&
				UserSettingsService.GeneralSettingsService.ShowOpenInNewTab)
			{
				_executableContextType = ExecutableContextType.ContentPageContext;
				return true;
			}
			else if (HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem is not null)
			{
				_executableContextType = ExecutableContextType.HomePageContext;
				return true;
			}
			else if (SidebarContext.IsItemRightClicked &&
				SidebarContext.RightClickedItem is not null)
			{
				_executableContextType = ExecutableContextType.SidebarContext;
				return true;
			}

			return false;
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
