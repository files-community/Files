// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Files.App.Controls
{
	/// <summary>
	/// A container that hosts <see cref="BladeItem"/> controls in a horizontal scrolling list
	/// Based on the Azure portal UI
	/// </summary>
	public partial class BladeView : ItemsControl
	{
		private ScrollViewer? _scrollViewer;

		private Dictionary<BladeItem, Size> _cachedBladeItemSizes = new Dictionary<BladeItem, Size>();

		/// <summary>
		/// Initializes a new instance of the <see cref="BladeView"/> class.
		/// </summary>
		public BladeView()
		{
			DefaultStyleKey = typeof(BladeView);

			Items.VectorChanged += ItemsVectorChanged;

			Loaded += (sender, e) => AdjustBladeItemSize();
			SizeChanged += (sender, e) => AdjustBladeItemSize();
		}

		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			CycleBlades();
			AdjustBladeItemSize();
		}

		/// <inheritdoc/>
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new BladeItem();
		}

		/// <inheritdoc/>
		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is BladeItem;
		}

		/// <inheritdoc/>
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			var blade = element as BladeItem;
			if (blade != null)
			{
				blade.VisibilityChanged += BladeOnVisibilityChanged;
				blade.ParentBladeView = this;
			}

			base.PrepareContainerForItemOverride(element, item);
			CycleBlades();
		}

		/// <inheritdoc/>
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			var blade = element as BladeItem;
			if (blade != null)
			{
				blade.VisibilityChanged -= BladeOnVisibilityChanged;
			}

			base.ClearContainerForItemOverride(element, item);
		}

		/// <summary>
		/// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
		/// </summary>
		/// <returns>An automation peer for this <see cref="BladeView"/>.</returns>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new BladeViewAutomationPeer(this);
		}

		private void CycleBlades()
		{
			ActiveBlades = new ObservableCollection<BladeItem>();
			foreach (var item in Items)
			{
				var blade = GetBladeItem(item);
				if (blade != null)
				{
					if (blade.IsOpen)
					{
						ActiveBlades.Add(blade);
					}
				}
			}
		}

		private BladeItem? GetBladeItem(object item)
		{
			return item as BladeItem ?? ContainerFromItem(item) as BladeItem;
		}

		private async void BladeOnVisibilityChanged(object? sender, Visibility visibility)
		{
			if (sender is not BladeItem blade)
			{
				return;
			}

			if (visibility == Visibility.Visible)
			{
				var item = ItemFromContainer(blade);
				if (item is null)
				{
					return;
				}

				Items.Remove(item);
				Items.Add(item);
				BladeOpened?.Invoke(this, blade);
				ActiveBlades.Add(blade);
				UpdateLayout();

				// Need to do this because of touch. See more information here: https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/760#issuecomment-276466464
				await DispatcherQueue.EnqueueAsync(
					() =>
					{
						var scrollViewer = GetScrollViewer();
						scrollViewer?.ChangeView(scrollViewer.ScrollableWidth, null, null);
					}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

				return;
			}

			BladeClosed?.Invoke(this, blade);
			ActiveBlades.Remove(blade);
		}

		private ScrollViewer? GetScrollViewer()
		{
			_scrollViewer ??= this.FindDescendant<ScrollViewer>();
			return _scrollViewer;
		}

		public void ScrollToEnd()
		{
			LayoutUpdated += OnLayoutUpdatedScrollToEnd;
		}

		private void OnLayoutUpdatedScrollToEnd(object? sender, object e)
		{
			LayoutUpdated -= OnLayoutUpdatedScrollToEnd;
			var scrollViewer = GetScrollViewer();
			scrollViewer?.ChangeView(scrollViewer.ScrollableWidth, null, null, false);
		}

		private void AdjustBladeItemSize()
		{
			// Adjust blade items to be full screen
			var scrollViewer = GetScrollViewer();
			if (BladeMode == BladeMode.Fullscreen && scrollViewer is not null)
			{
				foreach (var item in Items)
				{
					var blade = GetBladeItem(item);
					if (blade is null)
					{
						continue;
					}

					blade.Width = scrollViewer.ActualWidth;
					blade.Height = scrollViewer.ActualHeight;
				}
			}
		}

		private void ItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
		{
			if (BladeMode == BladeMode.Fullscreen)
			{
				var bladeItem = GetBladeItem(sender[(int)e.Index]);
				if (bladeItem != null)
				{
					if (!_cachedBladeItemSizes.ContainsKey(bladeItem))
					{
						// Execute change of blade item size when a blade item is added in Fullscreen mode
						_cachedBladeItemSizes.Add(bladeItem, new Size(bladeItem.Width, bladeItem.Height));
						AdjustBladeItemSize();
					}
				}
			}
			else if (e.CollectionChange == CollectionChange.ItemInserted)
			{
				UpdateLayout();
				// The following line doesn't work as expected due to the items not being fully loaded yet and thus the scrollable width not being accurate.
				//GetScrollViewer()?.ChangeView(_scrollViewer.ScrollableWidth, null, null);
			}
		}
	}
}
