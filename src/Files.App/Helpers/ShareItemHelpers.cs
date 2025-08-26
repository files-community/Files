// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Helpers
{
	public static class ShareItemHelpers
	{
		private static DataTransferManager _dataTransferManager;
		private static IEnumerable<ListedItem> _itemsToShare;

		public static bool IsItemShareable(ListedItem item)
			=> !item.IsHiddenItem &&
				(!item.IsShortcut || item.IsLinkItem) &&
				(item.PrimaryItemAttribute != StorageItemTypes.Folder || item.IsArchive);

		public static async Task ShareItemsAsync(IEnumerable<ListedItem> itemsToShare)
		{
			if (itemsToShare is null)
				return;

			_itemsToShare = itemsToShare;

			if (_dataTransferManager == null)
			{
				var interop = DataTransferManager.As<IDataTransferManagerInterop>();
				IntPtr result = interop.GetForWindow(MainWindow.Instance.WindowHandle, Win32PInvoke.DataTransferManagerInteropIID);
				_dataTransferManager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);
			}
			_dataTransferManager.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);
			_dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);

			try
			{
				var interop = DataTransferManager.As<IDataTransferManagerInterop>();
				interop.ShowShareUIForWindow(MainWindow.Instance.WindowHandle);
			}
			catch (Exception ex)
			{
				var errorDialog = new ContentDialog()
				{
					Title = Strings.FaildToShareItems.GetLocalizedResource(),
					Content = ex.Message,
					PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				};

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await errorDialog.TryShowAsync();
			}

			async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
			{
				DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
				List<IStorageItem> items = [];
				DataRequest dataRequest = args.Request;

				foreach (ListedItem item in _itemsToShare)
				{
					if (item is IShortcutItem shItem)
					{
						if (shItem.IsLinkItem && !string.IsNullOrEmpty(shItem.TargetPath))
						{
							dataRequest.Data.Properties.Title = string.Format(Strings.ShareDialogTitle.GetLocalizedResource(), item.Name);
							dataRequest.Data.Properties.Description = Strings.ShareDialogSingleItemDescription.GetLocalizedResource();
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
					dataRequest.Data.Properties.Title = string.Format(Strings.ShareDialogTitle.GetLocalizedResource(), items.First().Name);
					dataRequest.Data.Properties.Description = Strings.ShareDialogSingleItemDescription.GetLocalizedResource();
				}
				else if (items.Count == 0)
				{
					dataRequest.FailWithDisplayText(Strings.ShareDialogFailMessage.GetLocalizedResource());
					dataRequestDeferral.Complete();

					return;
				}
				else
				{
					dataRequest.Data.Properties.Title = string.Format(
						Strings.ShareDialogTitleMultipleItems.GetLocalizedResource(),
						items.Count,
						"ItemsCount.Text".GetLocalizedResource());
					dataRequest.Data.Properties.Description = Strings.ShareDialogMultipleItemsDescription.GetLocalizedResource();
				}

				dataRequest.Data.SetStorageItems(items, false);
				dataRequestDeferral.Complete();
			}
		}
	}
}
