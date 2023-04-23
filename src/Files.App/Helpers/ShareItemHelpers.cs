// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;

namespace Files.App.Helpers
{
	public static class ShareItemHelpers
	{
		public static bool IsItemShareable(ListedItem item)
			=> !item.IsHiddenItem &&
				(!item.IsShortcut || item.IsLinkItem) &&
				(item.PrimaryItemAttribute != StorageItemTypes.Folder || item.IsArchive);

		public static void ShareItems(IEnumerable<ListedItem> itemsToShare)
		{
			var interop = DataTransferManager.As<UWPToWinAppSDKUpgradeHelpers.IDataTransferManagerInterop>();
			IntPtr result = interop.GetForWindow(App.WindowHandle, UWPToWinAppSDKUpgradeHelpers.InteropHelpers.DataTransferManagerInteropIID);

			var manager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);
			manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);

			interop.ShowShareUIForWindow(App.WindowHandle);

			async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
			{
				DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
				List<IStorageItem> items = new();
				DataRequest dataRequest = args.Request;

				foreach (ListedItem item in itemsToShare)
				{
					if (item is ShortcutItem shItem)
					{
						if (shItem.IsLinkItem && !string.IsNullOrEmpty(shItem.TargetPath))
						{
							dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalizedResource(), item.Name);
							dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalizedResource();
							dataRequest.Data.SetWebLink(new Uri(shItem.TargetPath));
							dataRequestDeferral.Complete();

							return;
						}
					}
					else if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsArchive)
					{
						if (await StorageHelpers.ToStorageItem<BaseStorageFolder>(item.ItemPath) is BaseStorageFolder folder)
							items.Add(folder);
					}
					else
					{
						if (await StorageHelpers.ToStorageItem<BaseStorageFile>(item.ItemPath) is BaseStorageFile file)
							items.Add(file);
					}
				}

				if (items.Count == 1)
				{
					dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalizedResource(), items.First().Name);
					dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalizedResource();
				}
				else if (items.Count == 0)
				{
					dataRequest.FailWithDisplayText("ShareDialogFailMessage".GetLocalizedResource());
					dataRequestDeferral.Complete();

					return;
				}
				else
				{
					dataRequest.Data.Properties.Title = string.Format(
						"ShareDialogTitleMultipleItems".GetLocalizedResource(),
						items.Count,
						"ItemsCount.Text".GetLocalizedResource());
					dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".GetLocalizedResource();
				}

				dataRequest.Data.SetStorageItems(items, false);
				dataRequestDeferral.Complete();
			}
		}
	}
}
