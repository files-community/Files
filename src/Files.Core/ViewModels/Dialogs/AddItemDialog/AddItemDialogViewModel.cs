using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Core.Enums;
using Files.Core.Extensions;
using Files.Core.Models;
using Files.Core.Models.Dialogs;
using Files.Core.Services;
using Files.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.Core.ViewModels.Dialogs.AddItemDialog
{
	public sealed class AddItemDialogViewModel : ObservableObject
	{
		public IImageService ImagingService { get; } = Ioc.Default.GetRequiredService<IImageService>();

		public ObservableCollection<AddItemDialogListItemViewModel> AddItemsList { get; }

		public AddItemDialogResultModel ResultType { get; set; } = new AddItemDialogResultModel() { ItemType = AddItemDialogItemType.Cancel };

		public AddItemDialogViewModel()
		{
			AddItemsList = new();
		}

		public async Task AddItemsToList(IEnumerable<ShellNewEntry> itemTypes)
		{
			AddItemsList.Clear();

			AddItemsList.Add(new AddItemDialogListItemViewModel
			{
				Header = "Folder".ToLocalized(),
				SubHeader = "AddDialogListFolderSubHeader".ToLocalized(),
				Glyph = "\xE838",
				IsItemEnabled = true,
				ItemResult = new AddItemDialogResultModel() { ItemType = AddItemDialogItemType.Folder }
			});
			AddItemsList.Add(new AddItemDialogListItemViewModel
			{
				Header = "File".ToLocalized(),
				SubHeader = "AddDialogListFileSubHeader".ToLocalized(),
				Glyph = "\xE8A5",
				IsItemEnabled = true,
				ItemResult = new AddItemDialogResultModel()
				{
					ItemType = AddItemDialogItemType.File,
					ItemInfo = null
				}
			});
			AddItemsList.Add(new AddItemDialogListItemViewModel
			{
				Header = "Shortcut".ToLocalized(),
				SubHeader = "AddDialogListShortcutSubHeader".ToLocalized(),
				Glyph = "\uE71B",
				IsItemEnabled = true,
				ItemResult = new AddItemDialogResultModel()
				{
					ItemType = AddItemDialogItemType.Shortcut,
					ItemInfo = null
				}
			});

			foreach (var itemType in itemTypes)
			{
				IImageModel? imageModel = null;
				if (!string.IsNullOrEmpty(itemType.IconBase64))
				{
					byte[] bitmapData = Convert.FromBase64String(itemType.IconBase64);
					imageModel = await ImagingService.GetImageModelFromDataAsync(bitmapData);
				}

				AddItemsList.Add(new AddItemDialogListItemViewModel
				{
					Header = itemType.Name,
					SubHeader = itemType.Extension,
					Glyph = imageModel is not null ? null : "\xE8A5",
					Icon = imageModel,
					IsItemEnabled = true,
					ItemResult = new AddItemDialogResultModel()
					{
						ItemType = AddItemDialogItemType.File,
						ItemInfo = itemType
					}
				});
			}
		}
	}
}
