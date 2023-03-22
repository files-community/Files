using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class PasteItemToSelectionAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Paste".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconPaste");

		public HotKey HotKey { get; } = new(VirtualKey.V, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public PasteItemToSelectionAction()
		{
			isExecutable = GetIsExecutable();

			context.PropertyChanged += Context_PropertyChanged;
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is null)
				return;

			string path = context.SelectedItem is ListedItem selectedItem
				? selectedItem.ItemPath
				: context.ShellPage.FilesystemViewModel.WorkingDirectory;

			await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
		}

		public bool GetIsExecutable()
		{
			if (!App.AppModel.IsPasteEnabled)
				return false;
			if (context.PageType is ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.SearchResults)
				return false;
			if (!context.HasSelection)
				return true;
			return context.SelectedItem?.PrimaryItemAttribute is Windows.Storage.StorageItemTypes.Folder;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItem):
					SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
					break;
			}
		}
		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
				SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
		}
	}
}
