// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Text;

namespace Files.App.ViewModels.UserControls
{
	public class PathBreadcrumbViewModel
	{
		// Dependency injections

		private MainPageViewModel MainPageViewModel { get; } = Ioc.Default.GetRequiredService<MainPageViewModel>();

		// Event handlers

		public delegate void ToolbarPathItemLoadedEventHandler(object sender, ToolbarPathItemLoadedEventArgs e);
		public delegate void PathBreadcrumbItemDroppedEventHandler(object sender, PathBreadcrumbItemDroppedEventArgs e);
		public delegate void ToolbarFlyoutOpenedEventHandler(object sender, ToolbarFlyoutOpenedEventArgs e);
		public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event ToolbarPathItemLoadedEventHandler? ToolbarPathItemLoaded;
		public event PathBreadcrumbItemDroppedEventHandler? PathBreadcrumbItemDropped;
		public event IAddressToolbar.ItemDraggedOverPathItemEventHandler? ItemDraggedOverPathItem;
		public event ToolbarFlyoutOpenedEventHandler? ToolbarFlyoutOpened;
		public event ToolbarPathItemInvokedEventHandler? ToolbarPathItemInvoked;

		// Properties

		public ObservableCollection<PathBreadcrumbItem> PathBreadcrumbItems { get; } = new();

		public bool IsSingleItemOverride { get; set; } = false;

		private static AddressToolbar? AddressToolbar
			=> (MainWindow.Instance.Content as Frame)?.FindDescendant<AddressToolbar>();

		private readonly DispatcherQueue _dispatcherQueue;
		private readonly DispatcherQueueTimer _dragOverTimer;
		private PointerRoutedEventArgs? pointerRoutedEventArgs;
		private bool _lockFlag = false;
		private string? _dragOverPath = null;

		// Methods

		public PathBreadcrumbViewModel()
		{
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
			_dragOverTimer = _dispatcherQueue.CreateTimer();
		}

		public async Task SetPathBoxDropDownFlyoutAsync(MenuFlyout flyout, PathBreadcrumbItem pathItem, IShellPage shellPage)
		{
			var nextPathItemTitle = PathBreadcrumbItems[PathBreadcrumbItems.IndexOf(pathItem) + 1].Name;
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
				PathBreadcrumbItems[PathBreadcrumbItems.Count - 1].Path?.TrimEnd(Path.DirectorySeparatorChar);

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

		public void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			if (sender is not FontIcon pathSeparatorIcon || pathSeparatorIcon.DataContext is null)
				return;

			ToolbarPathItemLoaded?.Invoke(pathSeparatorIcon, new ToolbarPathItemLoadedEventArgs()
			{
				Item = (PathBreadcrumbItem)pathSeparatorIcon.DataContext,
				OpenedFlyout = (MenuFlyout)pathSeparatorIcon.ContextFlyout
			});
		}

		public void PathBreadcrumbItem_DragLeave(object sender, DragEventArgs e)
		{
			if (((StackPanel)sender).DataContext is not PathBreadcrumbItem pathBreadcrumbItem ||
				pathBreadcrumbItem.Path == "Home")
			{
				return;
			}

			// Reset dragged over PathBox item
			if (pathBreadcrumbItem.Path == _dragOverPath)
				_dragOverPath = null;
		}

		public async Task PathBreadcrumbItem_Drop(object sender, DragEventArgs e)
		{
			if (_lockFlag)
				return;

			_lockFlag = true;

			// Reset dragged over PathBox item
			_dragOverPath = null;

			if (((StackPanel)sender).DataContext is not PathBreadcrumbItem pathBreadcrumbItem ||
				pathBreadcrumbItem.Path == "Home")
			{
				return;
			}

			var deferral = e.GetDeferral();

			var signal = new AsyncManualResetEvent();

			PathBreadcrumbItemDropped?.Invoke(this, new PathBreadcrumbItemDroppedEventArgs()
			{
				AcceptedOperation = e.AcceptedOperation,
				Package = e.DataView,
				Path = pathBreadcrumbItem.Path,
				SignalEvent = signal
			});

			await signal.WaitAsync();

			deferral.Complete();
			await Task.Yield();

			_lockFlag = false;
		}

		public async Task PathBreadcrumbItem_DragOver(object sender, DragEventArgs e)
		{
			if (IsSingleItemOverride ||
				((StackPanel)sender).DataContext is not PathBreadcrumbItem pathBreadcrumbItem ||
				pathBreadcrumbItem.Path == "Home")
			{
				return;
			}

			if (_dragOverPath != pathBreadcrumbItem.Path)
			{
				_dragOverPath = pathBreadcrumbItem.Path;
				_dragOverTimer.Stop();

				if (_dragOverPath != PathBreadcrumbItems.LastOrDefault()?.Path)
				{
					_dragOverTimer.Debounce(() =>
					{
						if (_dragOverPath is not null)
						{
							_dragOverTimer.Stop();
							ItemDraggedOverPathItem?.Invoke(this, new PathNavigationEventArgs()
							{
								ItemPath = _dragOverPath
							});
							_dragOverPath = null;
						}
					},
					TimeSpan.FromMilliseconds(1000), false);
				}
			}

			// In search page
			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView) || string.IsNullOrEmpty(pathBreadcrumbItem.Path))
			{
				e.AcceptedOperation = DataPackageOperation.None;

				return;
			}

			e.Handled = true;
			var deferral = e.GetDeferral();

			var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if (!storageItems.Any(storageItem =>
					!string.IsNullOrEmpty(storageItem?.Path) &&
					storageItem.Path.Replace(pathBreadcrumbItem.Path, string.Empty, StringComparison.Ordinal)
						.Trim(Path.DirectorySeparatorChar)
						.Contains(Path.DirectorySeparatorChar)))
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}

			// Copy be default when dragging from zip
			else if (storageItems.Any(x =>
					x.Item is ZipStorageFile ||
					x.Item is ZipStorageFolder) ||
					ZipStorageFolder.IsZipPath(pathBreadcrumbItem.Path))
			{
				e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), pathBreadcrumbItem.Name);
				e.AcceptedOperation = DataPackageOperation.Copy;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), pathBreadcrumbItem.Name);
				e.AcceptedOperation = DataPackageOperation.Move;
			}

			deferral.Complete();
		}

		public void PathBreadcrumbItemFlyout_Opened(object sender, object e)
		{
			ToolbarFlyoutOpened?.Invoke(this, new ToolbarFlyoutOpenedEventArgs() { OpenedFlyout = (MenuFlyout)sender });
		}

		public void PathBreadcrumbItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse)
				return;

			var ptrPt = e.GetCurrentPoint(AddressToolbar);
			pointerRoutedEventArgs = ptrPt.Properties.IsMiddleButtonPressed ? e : null;
		}

		public async Task PathBreadcrumbItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var itemTappedPath = ((sender as TextBlock)?.DataContext as PathBreadcrumbItem)?.Path;
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
	}
}
