// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

#nullable disable warnings

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
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

		#endregion Singleton

		#region Private Members

		private readonly IShellPage associatedInstance;

		private readonly ItemManipulationModel itemManipulationModel;
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();


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

		public virtual async Task OpenDirectoryInNewTab(RoutedEventArgs e)
		{
			foreach (ListedItem listedItem in SlimContentPage.SelectedItems)
			{
				await App.Window.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
				},
				Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
		}

		public virtual void OpenDirectoryInNewPane(RoutedEventArgs e)
		{
			NavigationHelpers.OpenInSecondaryPane(associatedInstance, SlimContentPage.SelectedItems.FirstOrDefault());
		}

		public virtual async Task OpenInNewWindowItem(RoutedEventArgs e)
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

		public virtual async Task ItemPointerPressed(PointerRoutedEventArgs e)
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
					if (pwd.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
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

		#endregion Command Implementation
	}
}
