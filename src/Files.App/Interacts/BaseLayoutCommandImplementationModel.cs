#nullable disable warnings

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
using Files.App.Shell;
using Files.App.ViewModels;
using Files.App.Views;
using Files.Backend.Enums;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using static Files.App.Constants.Browser.GridViewBrowser;

namespace Files.App.Interacts
{
	/// <summary>
	/// This class provides default implementation for BaseLayout commands.
	/// This class can be also inherited from and functions overridden to provide custom functionality
	/// </summary>
	public class BaseLayoutCommandImplementationModel : IBaseLayoutCommandImplementationModel
	{
		#region Singleton

		private IBaseLayout SlimContentPage => associatedInstance?.SlimContentPage;

		private IFilesystemHelpers FilesystemHelpers => associatedInstance?.FilesystemHelpers;

		private static IQuickAccessService QuickAccessService => Ioc.Default.GetRequiredService<IQuickAccessService>();

		#endregion Singleton

		#region Private Members

		private readonly IShellPage associatedInstance;

		private readonly ItemManipulationModel itemManipulationModel;

		#endregion Private Members

		#region Constructor

		public BaseLayoutCommandImplementationModel(IShellPage associatedInstance, ItemManipulationModel itemManipulationModel)
		{
			this.associatedInstance = associatedInstance;
			this.itemManipulationModel = itemManipulationModel;
		}

		#endregion Constructor

		#region IDisposable

		public void Dispose()
		{
			//associatedInstance = null;
		}

		#endregion IDisposable

		#region Command Implementation

		public virtual void RenameItem(RoutedEventArgs e)
		{
			itemManipulationModel.StartRenameItem();
		}

		public virtual void ShowProperties(RoutedEventArgs e)
		{
			if (SlimContentPage.ItemContextMenuFlyout.IsOpen)
				SlimContentPage.ItemContextMenuFlyout.Closed += OpenPropertiesFromItemContextMenuFlyout;
			else if (SlimContentPage.BaseContextMenuFlyout.IsOpen)
				SlimContentPage.BaseContextMenuFlyout.Closed += OpenPropertiesFromBaseContextMenuFlyout;
			else
				FilePropertiesHelpers.ShowProperties(associatedInstance);
		}

		private void OpenPropertiesFromItemContextMenuFlyout(object sender, object e)
		{
			SlimContentPage.ItemContextMenuFlyout.Closed -= OpenPropertiesFromItemContextMenuFlyout;
			FilePropertiesHelpers.ShowProperties(associatedInstance);
		}

		private void OpenPropertiesFromBaseContextMenuFlyout(object sender, object e)
		{
			SlimContentPage.BaseContextMenuFlyout.Closed -= OpenPropertiesFromBaseContextMenuFlyout;
			FilePropertiesHelpers.ShowProperties(associatedInstance);
		}

		public virtual async void OpenFileLocation(RoutedEventArgs e)
		{
			ShortcutItem item = SlimContentPage.SelectedItem as ShortcutItem;

			if (string.IsNullOrWhiteSpace(item?.TargetPath))
				return;

			// Check if destination path exists
			string folderPath = Path.GetDirectoryName(item.TargetPath);
			FilesystemResult<StorageFolderWithPath> destFolder = await associatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);

			if (destFolder)
			{
				associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
				{
					NavPathParam = folderPath,
					SelectItems = new[] { Path.GetFileName(item.TargetPath.TrimPath()) },
					AssociatedTabInstance = associatedInstance
				});
			}
			else if (destFolder == FileSystemStatusCode.NotFound)
			{
				await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
			}
			else
			{
				await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalizedResource(),
					string.Format("InvalidItemDialogContent".GetLocalizedResource(), Environment.NewLine, destFolder.ErrorCode.ToString()));
			}
		}

		public virtual async void OpenDirectoryInNewTab(RoutedEventArgs e)
		{
			foreach (ListedItem listedItem in SlimContentPage.SelectedItems)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
				},
				Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
		}

		public virtual void OpenDirectoryInNewPane(RoutedEventArgs e)
		{
			NavigationHelpers.OpenInSecondaryPane(associatedInstance, SlimContentPage.SelectedItems.FirstOrDefault());
		}

		public virtual async void OpenInNewWindowItem(RoutedEventArgs e)
		{
			List<ListedItem> items = SlimContentPage.SelectedItems;

			foreach (ListedItem listedItem in items)
			{
				var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
				var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
				await Launcher.LaunchUriAsync(folderUri);
			}
		}

		public virtual void CreateNewFile(ShellNewEntry f)
		{
			UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.File, f, associatedInstance);
		}

		public virtual void ShareItem(RoutedEventArgs e)
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

				//dataRequest.Data.Properties.Title = "Data Shared From Files";
				//dataRequest.Data.Properties.Description = "The items you selected will be shared";

				foreach (ListedItem item in SlimContentPage.SelectedItems)
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
					dataRequest.Data.Properties.Title = string.Format("ShareDialogTitleMultipleItems".GetLocalizedResource(), items.Count,
						"ItemsCount.Text".GetLocalizedResource());
					dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".GetLocalizedResource();
				}

				dataRequest.Data.SetStorageItems(items, false);
				dataRequestDeferral.Complete();

				// TODO: Unhook the event somewhere
			}
		}

		public virtual async void ItemPointerPressed(PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
			{
				// If a folder item was clicked, disable middle mouse click to scroll to cancel the mouse scrolling state and re-enable it
				if (e.OriginalSource is FrameworkElement { DataContext: ListedItem Item } && Item.PrimaryItemAttribute == StorageItemTypes.Folder)
				{
					SlimContentPage.IsMiddleClickToScrollEnabled = false;
					SlimContentPage.IsMiddleClickToScrollEnabled = true;

					if (Item.IsShortcut)
						await NavigationHelpers.OpenPathInNewTab(((e.OriginalSource as FrameworkElement)?.DataContext as ShortcutItem)?.TargetPath ?? Item.ItemPath);
					else
						await NavigationHelpers.OpenPathInNewTab(Item.ItemPath);
				}
			}
		}

		public virtual void PointerWheelChanged(PointerRoutedEventArgs e)
		{
			if (e.KeyModifiers is VirtualKeyModifiers.Control)
			{
				if (associatedInstance.IsCurrentInstance)
				{
					int delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
					if (delta < 0) // Mouse wheel down
						associatedInstance.InstanceViewModel.FolderSettings.GridViewSize -= GridViewIncrement;
					else if (delta > 0) // Mouse wheel up
						associatedInstance.InstanceViewModel.FolderSettings.GridViewSize += GridViewIncrement;
				}

				e.Handled = true;
			}
		}

		public virtual async Task DragOver(DragEventArgs e)
		{
			var deferral = e.GetDeferral();

			if (associatedInstance.InstanceViewModel.IsPageTypeSearchResults)
			{
				e.AcceptedOperation = DataPackageOperation.None;
				deferral.Complete();
				return;
			}

			itemManipulationModel.ClearSelection();

			if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				e.Handled = true;

				var draggedItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

				var pwd = associatedInstance.FilesystemViewModel.WorkingDirectory.TrimPath();
				var folderName = (Path.IsPathRooted(pwd) && Path.GetPathRoot(pwd) == pwd) ? Path.GetPathRoot(pwd) : Path.GetFileName(pwd);

				// As long as one file doesn't already belong to this folder
				if (associatedInstance.InstanceViewModel.IsPageTypeSearchResults || (draggedItems.Any() && draggedItems.AreItemsAlreadyInFolder(associatedInstance.FilesystemViewModel.WorkingDirectory)))
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else if (!draggedItems.Any())
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else
				{
					e.DragUIOverride.IsCaptionVisible = true;
					if (pwd.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Move;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
					{
						e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Link;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
					{
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Copy;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
					{
						e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Move;
					}
					else if (draggedItems.Any(x =>
						x.Item is ZipStorageFile ||
						x.Item is ZipStorageFolder) ||
						ZipStorageFolder.IsZipPath(pwd))
					{
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Copy;
					}
					else if (draggedItems.AreItemsInSameDrive(associatedInstance.FilesystemViewModel.WorkingDirectory))
					{
						e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Move;
					}
					else
					{
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Copy;
					}
				}
			}

			deferral.Complete();
		}

		public virtual async Task Drop(DragEventArgs e)
		{
			var deferral = e.GetDeferral();

			if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				await associatedInstance.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, associatedInstance.FilesystemViewModel.WorkingDirectory, false, true);
				e.Handled = true;
			}

			deferral.Complete();
		}

		public virtual void RefreshItems(RoutedEventArgs e)
		{
			associatedInstance.Refresh_Click();
		}

		public void SearchUnindexedItems(RoutedEventArgs e)
		{
			associatedInstance.SubmitSearch(associatedInstance.InstanceViewModel.CurrentSearchQuery, true);
		}

		public async Task CreateFolderWithSelection(RoutedEventArgs e)
		{
			await UIFilesystemHelpers.CreateFolderWithSelectionAsync(associatedInstance);
		}

		public async Task PlayAll()
		{
			await NavigationHelpers.OpenSelectedItems(associatedInstance);
		}

		#endregion Command Implementation
	}
}
