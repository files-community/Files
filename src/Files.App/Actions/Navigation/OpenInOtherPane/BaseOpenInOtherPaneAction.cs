// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal abstract class BaseOpenInOtherPaneAction : ObservableObject, IAction
	{
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		public string Label
			=> Strings.OpenInOtherPane.GetLocalizedResource();

		public string Description
			=> Strings.OpenDirectoryInOtherPaneDescription.GetLocalizedResource();

		public virtual ActionCategory Category
			=> ActionCategory.DualPane;

		public virtual bool IsExecutable =>
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.SelectedItem is not null &&
			ContentPageContext.SelectedItem.IsFolder &&
			ContentPageContext.IsMultiPaneActive;

		public virtual bool IsAccessibleGlobally
			=> true;

		public BaseOpenInOtherPaneAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public virtual Task ExecuteAsync(object? parameter = null)
		{
			var selectedItem = ContentPageContext.ShellPage?.SlimContentPage?.SelectedItems?.FirstOrDefault();
			if (selectedItem is null)
				return Task.CompletedTask;

			var path = (selectedItem as IShortcutItem)?.TargetPath ?? selectedItem.ItemPath;
			ContentPageContext.ShellPage?.PaneHolder?.OpenInOtherPane(path);

			return Task.CompletedTask;
		}

		protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.IsMultiPaneActive):
				case nameof(IContentPageContext.IsMultiPaneAvailable):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
