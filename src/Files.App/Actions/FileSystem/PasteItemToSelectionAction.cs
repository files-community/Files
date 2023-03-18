﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class PasteItemToSelectionAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Paste".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconPaste");

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
			if (context.ShellPage is not null && context.SelectedItem is ListedItem selectedItem)
			{
				await UIFilesystemHelpers.PasteItemAsync(selectedItem.ItemPath, context.ShellPage);
			}
		}

		public bool GetIsExecutable()
		{
			return App.AppModel.IsPasteEnabled
				&& context.SelectedItem is ListedItem selectedItem
				&& selectedItem.PrimaryItemAttribute is Windows.Storage.StorageItemTypes.Folder
				&& context.PageType is not ContentPageTypes.Home and not ContentPageTypes.RecycleBin and not ContentPageTypes.SearchResults;
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
