// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Text;

namespace Files.App.ViewModels.UserControls
{
	public class PathBreadcrumbViewModel
	{
		private readonly MainPageViewModel MainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		private DispatcherQueue dispatcherQueue;
		private DispatcherQueueTimer dragOverTimer;

		public ObservableCollection<PathBoxItem> PathComponents { get; } = new();

		private AddressToolbar? AddressToolbar
			=> (MainWindow.Instance.Content as Frame)?.FindDescendant<AddressToolbar>();

		public delegate void ToolbarPathItemLoadedEventHandler(object sender, ToolbarPathItemLoadedEventArgs e);
		public event ToolbarPathItemLoadedEventHandler? ToolbarPathItemLoaded;
		public delegate void ToolbarFlyoutOpenedEventHandler(object sender, ToolbarFlyoutOpenedEventArgs e);
		public event ToolbarFlyoutOpenedEventHandler? ToolbarFlyoutOpened;
		private PointerRoutedEventArgs? pointerRoutedEventArgs;
		public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event ToolbarPathItemInvokedEventHandler? ToolbarPathItemInvoked;
		public delegate void PathBoxItemDroppedEventHandler(object sender, PathBoxItemDroppedEventArgs e);
		public event PathBoxItemDroppedEventHandler? PathBoxItemDropped;
		public event IAddressToolbar.ItemDraggedOverPathItemEventHandler? ItemDraggedOverPathItem;

		private string? dragOverPath = null;

		private bool lockFlag = false;

		public bool IsSingleItemOverride { get; set; } = false;

		public PathBreadcrumbViewModel()
		{
			dispatcherQueue = DispatcherQueue.GetForCurrentThread();
			dragOverTimer = dispatcherQueue.CreateTimer();
		}

		public void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var pathSeparatorIcon = sender as FontIcon;
			if (pathSeparatorIcon is null || pathSeparatorIcon.DataContext is null)
				return;

			ToolbarPathItemLoaded?.Invoke(pathSeparatorIcon, new ToolbarPathItemLoadedEventArgs()
			{
				Item = (PathBoxItem)pathSeparatorIcon.DataContext,
				OpenedFlyout = (MenuFlyout)pathSeparatorIcon.ContextFlyout
			});
		}

		public void PathboxItemFlyout_Opened(object sender, object e)
		{
			ToolbarFlyoutOpened?.Invoke(this, new ToolbarFlyoutOpenedEventArgs() { OpenedFlyout = (MenuFlyout)sender });
		}

		public async Task SetPathBoxDropDownFlyoutAsync(MenuFlyout flyout, PathBoxItem pathItem, IShellPage shellPage)
		{
			var nextPathItemTitle = PathComponents[PathComponents.IndexOf(pathItem) + 1].Title;

			IList<StorageFolderWithPath>? childFolders = null;

			StorageFolderWithPath folder = await shellPage.FilesystemViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
			if (folder is not null)
				childFolders = (await FilesystemTasks.Wrap(() => folder.GetFoldersWithPathAsync(string.Empty))).Result;

			flyout.Items?.Clear();

			if (childFolders is null || childFolders.Count == 0)
			{
				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon { Glyph = "\uE7BA" },
					Text = "SubDirectoryAccessDenied".GetLocalizedResource(),
					//Foreground = (SolidColorBrush)Application.Current.Resources["SystemControlErrorTextForegroundBrush"],
					FontSize = 12
				};

				flyout.Items?.Add(flyoutItem);

				return;
			}

			var boldFontWeight = new FontWeight { Weight = 800 };
			var normalFontWeight = new FontWeight { Weight = 400 };

			var workingPath =
				PathComponents[PathComponents.Count - 1].Path?.TrimEnd(Path.DirectorySeparatorChar);

			foreach (var childFolder in childFolders)
			{
				var isPathItemFocused = childFolder.Item.Name == nextPathItemTitle;

				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon
					{
						Glyph = "\uED25",
						FontWeight = isPathItemFocused ? boldFontWeight : normalFontWeight
					},
					Text = childFolder.Item.Name,
					FontSize = 12,
					FontWeight = isPathItemFocused ? boldFontWeight : normalFontWeight
				};

				if (workingPath != childFolder.Path)
				{
					flyoutItem.Click += (sender, args) =>
					{
						// Navigate to the directory
						shellPage.NavigateToPath(childFolder.Path);
					};
				}

				flyout.Items?.Add(flyoutItem);
			}
		}

		public void PathBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse)
				return;

			var ptrPt = e.GetCurrentPoint(AddressToolbar);
			pointerRoutedEventArgs = ptrPt.Properties.IsMiddleButtonPressed ? e : null;
		}

		public async Task PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var itemTappedPath = ((sender as TextBlock)?.DataContext as PathBoxItem)?.Path;
			if (itemTappedPath is null)
				return;

			if (pointerRoutedEventArgs is not null)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), itemTappedPath);
				}, DispatcherQueuePriority.Low);
				e.Handled = true;
				pointerRoutedEventArgs = null;

				return;
			}

			ToolbarPathItemInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = itemTappedPath
			});
		}

		public void PathBoxItem_DragLeave(object sender, DragEventArgs e)
		{
			if (((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home")
			{
				return;
			}

			// Reset dragged over pathbox item
			if (pathBoxItem.Path == dragOverPath)
				dragOverPath = null;
		}

		public async Task PathBoxItem_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
				return;

			lockFlag = true;

			// Reset dragged over pathbox item
			dragOverPath = null;

			if (((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home")
			{
				return;
			}

			var deferral = e.GetDeferral();

			var signal = new AsyncManualResetEvent();

			PathBoxItemDropped?.Invoke(this, new PathBoxItemDroppedEventArgs()
			{
				AcceptedOperation = e.AcceptedOperation,
				Package = e.DataView,
				Path = pathBoxItem.Path,
				SignalEvent = signal
			});

			await signal.WaitAsync();

			deferral.Complete();
			await Task.Yield();

			lockFlag = false;
		}

		public async Task PathBoxItem_DragOver(object sender, DragEventArgs e)
		{
			if (IsSingleItemOverride ||
				((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home")
			{
				return;
			}

			if (dragOverPath != pathBoxItem.Path)
			{
				dragOverPath = pathBoxItem.Path;
				dragOverTimer.Stop();

				if (dragOverPath != PathComponents.LastOrDefault()?.Path)
				{
					dragOverTimer.Debounce(() =>
					{
						if (dragOverPath is not null)
						{
							dragOverTimer.Stop();
							ItemDraggedOverPathItem?.Invoke(this, new PathNavigationEventArgs()
							{
								ItemPath = dragOverPath
							});
							dragOverPath = null;
						}
					},
					TimeSpan.FromMilliseconds(1000), false);
				}
			}

			// In search page
			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView) || string.IsNullOrEmpty(pathBoxItem.Path))
			{
				e.AcceptedOperation = DataPackageOperation.None;

				return;
			}

			e.Handled = true;
			var deferral = e.GetDeferral();

			var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if (!storageItems.Any(storageItem =>
					!string.IsNullOrEmpty(storageItem?.Path) &&
					storageItem.Path.Replace(pathBoxItem.Path, string.Empty, StringComparison.Ordinal)
						.Trim(Path.DirectorySeparatorChar)
						.Contains(Path.DirectorySeparatorChar)))
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}

			// Copy be default when dragging from zip
			else if (storageItems.Any(x =>
					x.Item is ZipStorageFile ||
					x.Item is ZipStorageFolder) ||
					ZipStorageFolder.IsZipPath(pathBoxItem.Path))
			{
				e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), pathBoxItem.Title);
				e.AcceptedOperation = DataPackageOperation.Copy;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), pathBoxItem.Title);
				e.AcceptedOperation = DataPackageOperation.Move;
			}

			deferral.Complete();
		}
	}
}
