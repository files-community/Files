using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Views;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class OpenItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "Open".GetLocalizedResource();

		public string Description => "OpenItemDescription".GetLocalizedResource();

		public RichGlyph Glyph => new(opacityStyle: "ColorIconOpenFile");

		public HotKey HotKey => new(Keys.Enter);

		private const int MaxOpenCount = 10;

		public bool IsExecutable => context.HasSelection && context.SelectedItems.Count <= MaxOpenCount &&
			!(context.ShellPage is ColumnShellPage && context.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder);

		public OpenItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			NavigationHelpers.OpenSelectedItems(context.ShellPage);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal class OpenItemWithApplicationPickerAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalizedResource();

		public string Description => "OpenItemWithApplicationPickerDescription".GetLocalizedResource();

		public RichGlyph Glyph => new(opacityStyle: "ColorIconOpenWith");

		public bool IsExecutable => context.HasSelection && context.SelectedItems.All(
				i => (i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) || (i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));

		public OpenItemWithApplicationPickerAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			NavigationHelpers.OpenSelectedItems(context.ShellPage, true);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal class OpenParentFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "BaseLayoutItemContextFlyoutOpenParentFolder/Text".GetLocalizedResource();

		public string Description => "OpenParentFolderDescription".GetLocalizedResource();

		public RichGlyph Glyph => new(baseGlyph: "\uE197");

		public bool IsExecutable => context.HasSelection && context.ShellPage.InstanceViewModel.IsPageTypeSearchResults;

		public OpenParentFolderAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var item = context.SelectedItem;
			var folderPath = Path.GetDirectoryName(item.ItemPath.TrimEnd('\\'));

			context.ShellPage.NavigateWithArguments(context.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
			{
				NavPathParam = folderPath,
				SelectItems = new[] { item.ItemNameRaw },
				AssociatedTabInstance = context.ShellPage
			});
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
