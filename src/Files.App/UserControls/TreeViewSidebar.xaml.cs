// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.IO;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls
{
	public sealed partial class TreeViewSidebar : UserControl
	{
		// Lazy DI resolution — eager field-init resolves singletons during MainPage XAML parsing, before MainPage's constructor body has finished. That ordering caused process-level crashes.
		private readonly Lazy<TreeViewSidebarViewModel> _viewModel = new(() => Ioc.Default.GetRequiredService<TreeViewSidebarViewModel>());

		[GeneratedDependencyProperty]
		public partial TreeViewSidebarViewModel? ViewModel { get; set; }

		private bool _isUnloaded;

		public TreeViewSidebar()
		{
			InitializeComponent();

			// TreeView's internal control template falls through to {Binding Children} on its inherited DataContext when our binding hasn't fully primed it. From MainPage, that's MainPageViewModel (no Children property) — leaving inheritance in place was the source of native AccessViolation crashes.
			Tree.DataContext = null;
		}

		partial void OnViewModelChanged(TreeViewSidebarViewModel? newValue)
		{
			if (newValue is null)
				return;
			newValue.PropertyChanged += OnViewModelPropertyChanged;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					if (_isUnloaded)
						return;
					ViewModel = _viewModel.Value;
					ViewModel.OnControlLoaded();
				}
				catch (Exception ex)
				{
					App.Logger?.LogWarning(ex, "TreeViewSidebar: Loaded init failed");
				}
			});
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_isUnloaded = true;
			try
			{
				if (_viewModel.IsValueCreated)
				{
					_viewModel.Value.PropertyChanged -= OnViewModelPropertyChanged;
					_viewModel.Value.OnControlUnloaded();
				}
			}
			// Cleanup must never throw; the page is being torn down
			catch (Exception ex)
			{
				App.Logger?.LogDebug(ex, "TreeViewSidebar: Unloaded cleanup failed");
			}
		}

		private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(TreeViewSidebarViewModel.SelectedNode))
				DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, ScrollSelectedIntoView);
		}

		private void ScrollSelectedIntoView()
		{
			var node = ViewModel?.SelectedNode;
			if (node is null || _isUnloaded)
				return;

			// Re-assert selection: WinUI resets TreeViewItem.IsSelected during container realization (PrepareContainerForItemOverride),
			// and the TwoWay binding propagates that false back to FolderNode.IsSelected before this dispatch runs.
			node.IsSelected = true;
			// The inner TreeViewItem in the DataTemplate carries Tag=FolderNode — find it and ask the layout system to scroll it into view.
			var item = DependencyObjectHelpers.FindChild<TreeViewItem>(Tree, tvi => ReferenceEquals(tvi.Tag, node));
			item?.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false, VerticalAlignmentRatio = 0.5 });
		}

		private async void Tree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
		{
			try
			{
				if (args.Node.Content is not FolderNode fn || ViewModel is null)
					return;
				await ViewModel.LoadChildrenAsync(fn);
				ViewModel.UpdateSelectionFromCurrentPath();
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: lazy expand failed");
			}
		}

		private void Tree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			if (args.InvokedItem is not FolderNode fn)
				return;
			ViewModel?.HandleItemInvoked(fn);
		}

		private async void Item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement el || el.Tag is not FolderNode fn)
				return;
			if (fn.Kind != FolderNodeKind.Folder)
				return;
			if (!fn.IsExpanded && fn.HasUnrealizedChildren && ViewModel is not null)
				await ViewModel.LoadChildrenAsync(fn);
			fn.IsExpanded = !fn.IsExpanded;
			e.Handled = true;
		}

		private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement el || el.Tag is not FolderNode fn || fn.IsSection)
				return;

			if (fn.SourceItem is not null)
			{
				ViewModel?.HandleItemContextInvoked(el, new ItemContextInvokedArgs(fn.SourceItem, e.GetPosition(el)));
				e.Handled = true;
				return;
			}

			var flyout = new MenuFlyout();

			var open = new MenuFlyoutItem { Text = Strings.Open.GetLocalizedResource() };
			open.Click += (_, _) => ViewModel?.NavigateToPath(fn.Path);
			flyout.Items.Add(open);

			var openNewTab = new MenuFlyoutItem { Text = Strings.OpenInNewTab.GetLocalizedResource() };
			openNewTab.Click += async (_, _) => await NavigationHelpers.OpenPathInNewTab(fn.Path, true);
			flyout.Items.Add(openNewTab);

			var copyPath = new MenuFlyoutItem { Text = Strings.CopyPath.GetLocalizedResource() };
			copyPath.Click += (_, _) =>
			{
				var dp = new DataPackage();
				dp.SetText(fn.Path);
				Clipboard.SetContent(dp);
			};
			flyout.Items.Add(copyPath);

			flyout.Items.Add(new MenuFlyoutSeparator());

			var openExplorer = new MenuFlyoutItem { Text = "Open in File Explorer" };
			openExplorer.Click += (_, _) =>
			{
				// Win32Exception from Process.Start when the shell launch fails (invalid path or denied access)
				try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = fn.Path, UseShellExecute = true }); }
				catch (System.ComponentModel.Win32Exception) { }
				catch (InvalidOperationException) { }
				catch (FileNotFoundException) { }
			};
			flyout.Items.Add(openExplorer);

			flyout.ShowAt(el, new FlyoutShowOptions { Position = e.GetPosition(el) });
			e.Handled = true;
		}
	}
}
