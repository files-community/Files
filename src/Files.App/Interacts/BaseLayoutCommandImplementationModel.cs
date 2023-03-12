#nullable disable warnings

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Archive;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
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
using System.Text;
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

		public virtual void OpenItem(RoutedEventArgs e)
		{
			_ = NavigationHelpers.OpenSelectedItems(associatedInstance, false);
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
			_ = NavigationHelpers.OpenSelectedItems(associatedInstance, true);
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
			if (e.KeyModifiers == VirtualKeyModifiers.Control)
			{
				// Mouse wheel down
				if (e.GetCurrentPoint(null).Properties.MouseWheelDelta < 0)
					GridViewSizeDecrease(null);
				// Mouse wheel up
				else
					GridViewSizeIncrease(null);

				e.Handled = true;
			}
		}

		public virtual void GridViewSizeDecrease(KeyboardAcceleratorInvokedEventArgs e)
		{
			// Make Smaller
			if (associatedInstance.IsCurrentInstance)
				associatedInstance.InstanceViewModel.FolderSettings.GridViewSize = associatedInstance.InstanceViewModel.FolderSettings.GridViewSize - Constants.Browser.GridViewBrowser.GridViewIncrement;

			if (e is not null)
				e.Handled = true;
		}

		public virtual void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e)
		{
			// Make Larger
			if (associatedInstance.IsCurrentInstance)
				associatedInstance.InstanceViewModel.FolderSettings.GridViewSize = associatedInstance.InstanceViewModel.FolderSettings.GridViewSize + Constants.Browser.GridViewBrowser.GridViewIncrement;

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

		public async Task DecompressArchive()
		{
			BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage.SelectedItems.Count != 0
				? associatedInstance.SlimContentPage.SelectedItem.ItemPath
				: associatedInstance.FilesystemViewModel.WorkingDirectory);

			if (archive is null)
				return;

			var isArchiveEncrypted = await FilesystemTasks.Wrap(() => ZipHelpers.IsArchiveEncrypted(archive));
			var password = string.Empty;

			DecompressArchiveDialog decompressArchiveDialog = new();
			DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
			{
				IsArchiveEncrypted = isArchiveEncrypted,
				ShowPathSelection = true
			};
			decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

			ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
			if (option != ContentDialogResult.Primary)
				return;

			if (isArchiveEncrypted)
				password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);

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

			await ExtractArchive(archive, destinationFolder, password);

			if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
				await NavigationHelpers.OpenPath(destinationFolderPath, associatedInstance, FilesystemItemType.Directory);
		}

		public async Task DecompressArchiveHere()
		{
			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				var password = string.Empty;
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);

				if (await FilesystemTasks.Wrap(() => ZipHelpers.IsArchiveEncrypted(archive)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};

					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				await ExtractArchive(archive, currentFolder, password);
			}
		}

		public async Task DecompressArchiveToChildFolder()
		{
			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				var password = string.Empty;

				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);
				BaseStorageFolder destinationFolder = null;

				if (await FilesystemTasks.Wrap(() => ZipHelpers.IsArchiveEncrypted(archive)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};
					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				if (currentFolder is not null)
					destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());

				await ExtractArchive(archive, destinationFolder, password);
			}
		}

		private static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder? destinationFolder, string password)
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

			await FilesystemTasks.Wrap(() => ZipHelpers.ExtractArchive(archive, destinationFolder, password, banner.ProgressEventSource, extractCancellation.Token));

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

		public Task InstallFont()
		{
			foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
				Win32API.InstallFont(selectedItem.ItemPath);

			return Task.CompletedTask;
		}

		public async Task PlayAll()
		{
			await NavigationHelpers.OpenSelectedItems(associatedInstance);
		}

		public void FormatDrive(ListedItem? e)
		{
			Win32API.OpenFormatDriveDialog(e?.ItemPath ?? string.Empty);
		}

		#endregion Command Implementation
	}
}
