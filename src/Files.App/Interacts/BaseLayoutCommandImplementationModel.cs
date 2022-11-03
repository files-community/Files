#nullable disable warnings

using CommunityToolkit.WinUI;
using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.Shell;
using Files.App.ViewModels;
using Files.App.ViewModels.Dialogs;
using Files.App.Views;
using Files.Backend.Enums;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.System;

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

		public virtual async void CreateShortcut(RoutedEventArgs e)
		{
			foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
			{
				var filePath = Path.Combine(associatedInstance.FilesystemViewModel.WorkingDirectory,
								string.Format("ShortcutCreateNewSuffix".GetLocalizedResource(), selectedItem.Name) + ".lnk");

				await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, selectedItem.ItemPath);
			}
		}

		public virtual void SetAsLockscreenBackgroundItem(RoutedEventArgs e)
		{
			WallpaperHelpers.SetAsBackground(WallpaperType.LockScreen, SlimContentPage.SelectedItem.ItemPath);
		}

		public virtual void SetAsDesktopBackgroundItem(RoutedEventArgs e)
		{
			WallpaperHelpers.SetAsBackground(WallpaperType.Desktop, SlimContentPage.SelectedItem.ItemPath);
		}

		public virtual void SetAsSlideshowItem(RoutedEventArgs e)
		{
			var images = (from o in SlimContentPage.SelectedItems select o.ItemPath).ToArray();
			WallpaperHelpers.SetSlideshow(images);
		}

		public virtual async void RunAsAdmin(RoutedEventArgs e)
		{
			await ContextMenu.InvokeVerb("runas", SlimContentPage.SelectedItem.ItemPath);
		}

		public virtual async void RunAsAnotherUser(RoutedEventArgs e)
		{
			await ContextMenu.InvokeVerb("runasuser", SlimContentPage.SelectedItem.ItemPath);
		}

		public virtual void SidebarPinItem(RoutedEventArgs e)
		{
			SidebarHelpers.PinItems(SlimContentPage.SelectedItems);
		}

		public virtual void SidebarUnpinItem(RoutedEventArgs e)
		{
			SidebarHelpers.UnpinItems(SlimContentPage.SelectedItems);
		}

		public virtual void OpenItem(RoutedEventArgs e)
		{
			NavigationHelpers.OpenSelectedItems(associatedInstance, false);
		}

		public virtual void UnpinDirectoryFromFavorites(RoutedEventArgs e)
		{
			App.SidebarPinnedController.Model.RemoveItem(associatedInstance.FilesystemViewModel.WorkingDirectory);
		}

		public virtual async void EmptyRecycleBin(RoutedEventArgs e)
		{
			await RecycleBinHelpers.S_EmptyRecycleBin();
		}

		public virtual async void RestoreRecycleBin(RoutedEventArgs e)
		{
			await RecycleBinHelpers.S_RestoreRecycleBin(associatedInstance);
		}

		public virtual async void RestoreSelectionRecycleBin(RoutedEventArgs e)
		{
			await RecycleBinHelpers.S_RestoreSelectionRecycleBin(associatedInstance);
		}

		public virtual async void QuickLook(RoutedEventArgs e)
		{
			await QuickLookHelpers.ToggleQuickLook(associatedInstance);
		}

		public virtual async void CopyItem(RoutedEventArgs e)
		{
			await UIFilesystemHelpers.CopyItem(associatedInstance);
		}

		public virtual void CutItem(RoutedEventArgs e)
		{
			UIFilesystemHelpers.CutItem(associatedInstance);
		}

		public virtual async void RestoreItem(RoutedEventArgs e)
		{
			await RecycleBinHelpers.S_RestoreItem(associatedInstance);
		}

		public virtual async void DeleteItem(RoutedEventArgs e)
		{
			await RecycleBinHelpers.S_DeleteItem(associatedInstance);
		}

		public virtual void ShowFolderProperties(RoutedEventArgs e)
		{
			SlimContentPage.ItemContextMenuFlyout.Closed += OpenProperties;
		}

		public virtual void ShowProperties(RoutedEventArgs e)
		{
			if (SlimContentPage.ItemContextMenuFlyout.IsOpen)
				SlimContentPage.ItemContextMenuFlyout.Closed += OpenProperties;
			else
				FilePropertiesHelpers.ShowProperties(associatedInstance);
		}

		private void OpenProperties(object sender, object e)
		{
			SlimContentPage.ItemContextMenuFlyout.Closed -= OpenProperties;
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

		public virtual void OpenParentFolder(RoutedEventArgs e)
		{
			var item = SlimContentPage.SelectedItem;
			var folderPath = Path.GetDirectoryName(item.ItemPath.TrimEnd('\\'));
			associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
			{
				NavPathParam = folderPath,
				SelectItems = new[] { item.ItemNameRaw },
				AssociatedTabInstance = associatedInstance
			});
		}

		public virtual void OpenItemWithApplicationPicker(RoutedEventArgs e)
		{
			NavigationHelpers.OpenSelectedItems(associatedInstance, true);
		}

		public virtual async void OpenDirectoryInNewTab(RoutedEventArgs e)
		{
			foreach (ListedItem listedItem in SlimContentPage.SelectedItems)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
				}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
		}

		public virtual void OpenDirectoryInNewPane(RoutedEventArgs e)
		{
			ListedItem listedItem = SlimContentPage.SelectedItems.FirstOrDefault();
			if (listedItem is not null)
				associatedInstance.PaneHolder?.OpenPathInNewPane((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
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

		public virtual void CreateNewFolder(RoutedEventArgs e)
		{
			UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null, associatedInstance);
		}

		public virtual void CreateNewFile(ShellNewEntry f)
		{
			UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.File, f, associatedInstance);
		}

		public virtual async void PasteItemsFromClipboard(RoutedEventArgs e)
		{
			if (SlimContentPage.SelectedItems.Count == 1 && SlimContentPage.SelectedItems.Single().PrimaryItemAttribute == StorageItemTypes.Folder)
				await UIFilesystemHelpers.PasteItemAsync(SlimContentPage.SelectedItems.Single().ItemPath, associatedInstance);
			else
				await UIFilesystemHelpers.PasteItemAsync(associatedInstance.FilesystemViewModel.WorkingDirectory, associatedInstance);
		}

		public virtual void CopyPathOfSelectedItem(RoutedEventArgs e)
		{
			try
			{
				if (SlimContentPage is not null)
				{
					var path = SlimContentPage.SelectedItem is not null ? SlimContentPage.SelectedItem.ItemPath : associatedInstance.FilesystemViewModel.WorkingDirectory;
					if (FtpHelpers.IsFtpPath(path))
						path = path.Replace("\\", "/", StringComparison.Ordinal);
					DataPackage data = new();
					data.SetText(path);
					Clipboard.SetContent(data);
					Clipboard.Flush();
				}
			}
			catch (Exception)
			{
				Debugger.Break();
			}
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

				/*dataRequest.Data.Properties.Title = "Data Shared From Files";
                dataRequest.Data.Properties.Description = "The items you selected will be shared";*/

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

		public virtual void PinDirectoryToFavorites(RoutedEventArgs e)
		{
			App.SidebarPinnedController.Model.AddItem(associatedInstance.FilesystemViewModel.WorkingDirectory);
		}

		public virtual async void ItemPointerPressed(PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
			{
				if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem Item && Item.PrimaryItemAttribute == StorageItemTypes.Folder)
				{
					// If a folder item was clicked, disable middle mouse click to scroll to cancel the mouse scrolling state and re-enable it
					SlimContentPage.IsMiddleClickToScrollEnabled = false;
					SlimContentPage.IsMiddleClickToScrollEnabled = true;

					if (Item.IsShortcut)
						await NavigationHelpers.OpenPathInNewTab(((e.OriginalSource as FrameworkElement)?.DataContext as ShortcutItem)?.TargetPath ?? Item.ItemPath);
					else
						await NavigationHelpers.OpenPathInNewTab(Item.ItemPath);
				}
			}
		}

		public virtual async void UnpinItemFromStart(RoutedEventArgs e)
		{
			if (associatedInstance.SlimContentPage.SelectedItems.Count > 0)
			{
				foreach (ListedItem listedItem in associatedInstance.SlimContentPage.SelectedItems)
					await App.SecondaryTileHelper.UnpinFromStartAsync(listedItem.ItemPath);
			}
			else
			{
				await App.SecondaryTileHelper.UnpinFromStartAsync(associatedInstance.FilesystemViewModel.WorkingDirectory);
			}
		}

		public async void PinItemToStart(RoutedEventArgs e)
		{
			if (associatedInstance.SlimContentPage.SelectedItems.Count > 0)
			{
				foreach (ListedItem listedItem in associatedInstance.SlimContentPage.SelectedItems)
					await App.SecondaryTileHelper.TryPinFolderAsync(listedItem.ItemPath, listedItem.Name);
			}
			else
			{
				await App.SecondaryTileHelper.TryPinFolderAsync(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath, associatedInstance.FilesystemViewModel.CurrentFolder.Name);
			}
		}

		public virtual void PointerWheelChanged(PointerRoutedEventArgs e)
		{
			if (e.KeyModifiers == VirtualKeyModifiers.Control)
			{
				if (e.GetCurrentPoint(null).Properties.MouseWheelDelta < 0) // Mouse wheel down
					GridViewSizeDecrease(null);
				else // Mouse wheel up
					GridViewSizeIncrease(null);

				e.Handled = true;
			}
		}

		public virtual void GridViewSizeDecrease(KeyboardAcceleratorInvokedEventArgs e)
		{
			if (associatedInstance.IsCurrentInstance)
				associatedInstance.InstanceViewModel.FolderSettings.GridViewSize = associatedInstance.InstanceViewModel.FolderSettings.GridViewSize - Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Smaller
			if (e is not null)
				e.Handled = true;
		}

		public virtual void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e)
		{
			if (associatedInstance.IsCurrentInstance)
				associatedInstance.InstanceViewModel.FolderSettings.GridViewSize = associatedInstance.InstanceViewModel.FolderSettings.GridViewSize + Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Larger
			if (e is not null)
				e.Handled = true;
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

				var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
				var draggedItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

				var pwd = associatedInstance.FilesystemViewModel.WorkingDirectory.TrimPath();
				var folderName = (Path.IsPathRooted(pwd) && Path.GetPathRoot(pwd) == pwd) ? Path.GetPathRoot(pwd) : Path.GetFileName(pwd);

				// As long as one file doesn't already belong to this folder
				if (associatedInstance.InstanceViewModel.IsPageTypeSearchResults || (draggedItems.Any() && draggedItems.AreItemsAlreadyInFolder(associatedInstance.FilesystemViewModel.WorkingDirectory)))
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else if (handledByFtp)
				{
					if (pwd.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						e.DragUIOverride.IsCaptionVisible = true;
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
						e.AcceptedOperation = DataPackageOperation.Copy;
					}
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
					else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
						|| ZipStorageFolder.IsZipPath(pwd))
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

		public async Task CompressIntoArchive()
		{
			string archivePath;
			string[] sources = associatedInstance.SlimContentPage.SelectedItems
				.Select(item => item.ItemPath)
				.ToArray();

			if (sources.Length == 1)
				archivePath = sources[0] + ".zip";
			else
			{
				DynamicDialog archiveDialog = DynamicDialogFactory.GetFor_RenameDialog();
				await archiveDialog.ShowAsync();
				if (archiveDialog.DynamicResult != DynamicDialogResult.Primary)
					return;
				archivePath = Path.Combine(
					associatedInstance.FilesystemViewModel.WorkingDirectory,
					$"{(string)archiveDialog.ViewModel.AdditionalData}.zip");
			}

			CancellationTokenSource compressionToken = new();
			PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner(
				"CompressionInProgress".GetLocalizedResource(),
				archivePath,
				0,
				ReturnResult.InProgress,
				FileOperationType.Compressed,
				compressionToken);

			bool result = await ZipHelpers.CompressMultipleToArchive(sources, archivePath, banner.Progress);

			banner.Remove();
			if (result)
				App.OngoingTasksViewModel.PostBanner(
					"CompressionCompleted".GetLocalizedResource(),
					string.Format("CompressionSucceded".GetLocalizedResource(), archivePath),
					0,
					ReturnResult.Success,
					FileOperationType.Compressed);
			else
				App.OngoingTasksViewModel.PostBanner(
					"CompressionCompleted".GetLocalizedResource(),
					string.Format("CompressionFailed".GetLocalizedResource(), archivePath),
					0,
					ReturnResult.Failed,
					FileOperationType.Compressed);
		}

		public async Task DecompressArchive()
		{
			BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage.SelectedItems.Count != 0
				? associatedInstance.SlimContentPage.SelectedItem.ItemPath
				: associatedInstance.FilesystemViewModel.WorkingDirectory);

			if (archive is null)
				return;

			DecompressArchiveDialog decompressArchiveDialog = new();
			DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive);
			decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

			ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
			if (option != ContentDialogResult.Primary)
				return;

			// Check if archive still exists
			if (!StorageHelpers.Exists(archive.Path))
				return;

			BaseStorageFolder destinationFolder = decompressArchiveViewModel.DestinationFolder;
			string destinationFolderPath = decompressArchiveViewModel.DestinationFolderPath;

			if (destinationFolder is null)
			{
				BaseStorageFolder parentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(Path.GetDirectoryName(archive.Path));
				destinationFolder = await FilesystemTasks.Wrap(() => parentFolder.CreateFolderAsync(Path.GetFileName(destinationFolderPath), CreationCollisionOption.GenerateUniqueName).AsTask());
			}

			await ExtractArchive(archive, destinationFolder);

			if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
				await NavigationHelpers.OpenPath(destinationFolderPath, associatedInstance, FilesystemItemType.Directory);
		}

		public async Task DecompressArchiveHere()
		{
			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);

				await ExtractArchive(archive, currentFolder);
			}
		}

		public async Task DecompressArchiveToChildFolder()
		{
			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);
				BaseStorageFolder destinationFolder = null;

				if (currentFolder is not null)
					destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());

				await ExtractArchive(archive, destinationFolder);
			}
		}

		private static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder destinationFolder)
		{
			if (archive is null || destinationFolder is null)
				return;

			CancellationTokenSource extractCancellation = new();
			PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner(
				archive.Name.Length >= 30 ? archive.Name + "\n" : archive.Name,
				"ExtractingArchiveText".GetLocalizedResource(),
				0,
				ReturnResult.InProgress,
				FileOperationType.Extract,
				extractCancellation);

			Stopwatch sw = new();
			sw.Start();

			await FilesystemTasks.Wrap(() => ZipHelpers.ExtractArchive(archive, destinationFolder, banner.Progress, extractCancellation.Token));

			sw.Stop();
			banner.Remove();

			if (sw.Elapsed.TotalSeconds >= 6)
			{
				App.OngoingTasksViewModel.PostBanner(
					"ExtractingCompleteText".GetLocalizedResource(),
					"ArchiveExtractionCompletedSuccessfullyText".GetLocalizedResource(),
					0,
					ReturnResult.Success,
					FileOperationType.Extract);
			}
		}

		public async Task InstallInfDriver()
		{
			foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
				await Win32API.InstallInf(selectedItem.ItemPath);
		}

		public async Task RotateImageLeft()
		{
			foreach (var image in SlimContentPage.SelectedItems)
				await BitmapHelper.Rotate(PathNormalization.NormalizePath(image.ItemPath), BitmapRotation.Clockwise270Degrees);

			SlimContentPage.ItemManipulationModel.RefreshItemsThumbnail();
			App.PreviewPaneViewModel.UpdateSelectedItemPreview();
		}

		public async Task RotateImageRight()
		{
			foreach (var image in SlimContentPage.SelectedItems)
				await BitmapHelper.Rotate(PathNormalization.NormalizePath(image.ItemPath), BitmapRotation.Clockwise90Degrees);

			SlimContentPage.ItemManipulationModel.RefreshItemsThumbnail();
			App.PreviewPaneViewModel.UpdateSelectedItemPreview();
		}

		public Task InstallFont()
		{
			foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
				Win32API.InstallFont(selectedItem.ItemPath);

			return Task.CompletedTask;
		}

		#endregion Command Implementation
	}
}