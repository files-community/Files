// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.Utils;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Provides middle-click auto-scroll for list controls backed by a <see cref="ScrollViewer"/>.
	/// </summary>
	public sealed class ScrollViewerMiddleClickExtensions : DependencyObject
	{
		private static readonly ConditionalWeakTable<FrameworkElement, MiddleClickScrollController> Controllers = new();

		public static bool GetEnableMiddleClickScrolling(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnableMiddleClickScrollingProperty);
		}

		public static void SetEnableMiddleClickScrolling(DependencyObject obj, bool value)
		{
			obj.SetValue(EnableMiddleClickScrollingProperty, value);
		}

		public static readonly DependencyProperty EnableMiddleClickScrollingProperty =
			DependencyProperty.RegisterAttached(
				"EnableMiddleClickScrolling",
				typeof(bool),
				typeof(ScrollViewerMiddleClickExtensions),
				new PropertyMetadata(false, OnEnableMiddleClickScrollingChanged));

		private static void OnEnableMiddleClickScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not FrameworkElement element)
				return;

			var controller = Controllers.GetValue(element, key => new MiddleClickScrollController(key));

			if ((bool)e.NewValue)
				controller.Enable();
			else
				controller.Disable();
		}

		private sealed class MiddleClickScrollController
		{
			private const double DeadZone = 12;
			private const double SpeedFactor = 0.12;
			private const double MaxSpeedPerTick = 32;
			private static readonly InputSystemCursor AutoScrollCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
			private static readonly InputSystemCursor DefaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

			private readonly FrameworkElement _element;
			private readonly DispatcherQueueTimer _scrollTimer;
			private readonly PointerEventHandler _rootPointerMovedHandler;
			private readonly PointerEventHandler _rootPointerPressedHandler;
			private readonly PointerEventHandler _rootPointerReleasedHandler;
			private readonly KeyEventHandler _rootKeyDownHandler;

			private UIElement? _rootElement;
			private ScrollViewer? _scrollViewer;

			private bool _isEnabled;
			private bool _isAutoScrollActive;
			private ulong _activationPressTimestamp;
			private bool _ignoreActivationMiddleRelease;
			private bool _holdScrollDetected;
			private UIElement? _lastCursorTarget;

			private Point _anchorPosition;
			private Point _currentPosition;

			public MiddleClickScrollController(FrameworkElement element)
			{
				_element = element;

				_scrollTimer = _element.DispatcherQueue.CreateTimer();
				_scrollTimer.Interval = TimeSpan.FromMilliseconds(16);
				_scrollTimer.Tick += ScrollTimer_Tick;

				_rootPointerMovedHandler = new PointerEventHandler(RootElement_PointerMoved);
				_rootPointerPressedHandler = new PointerEventHandler(RootElement_PointerPressed);
				_rootPointerReleasedHandler = new PointerEventHandler(RootElement_PointerReleased);
				_rootKeyDownHandler = new KeyEventHandler(RootElement_KeyDown);
			}

			public void Enable()
			{
				if (_isEnabled)
					return;

				_isEnabled = true;
				_element.Loaded += Element_Loaded;
				_element.Unloaded += Element_Unloaded;
				_element.PointerPressed += Element_PointerPressed;

				if (_element.IsLoaded)
					AttachToVisualTree();
			}

			public void Disable()
			{
				if (!_isEnabled)
					return;

				_isEnabled = false;
				StopAutoScroll();
				DetachFromVisualTree();

				_element.Loaded -= Element_Loaded;
				_element.Unloaded -= Element_Unloaded;
				_element.PointerPressed -= Element_PointerPressed;
			}

			private void Element_Loaded(object sender, RoutedEventArgs e)
			{
				AttachToVisualTree();
			}

			private void Element_Unloaded(object sender, RoutedEventArgs e)
			{
				StopAutoScroll();
				DetachFromVisualTree();
			}

			private void AttachToVisualTree()
			{
				_scrollViewer = _element.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer")
					?? _element.FindDescendant<ScrollViewer>();

				var root = MainWindow.Instance?.Content as UIElement;
				if (root is null || ReferenceEquals(_rootElement, root))
					return;

				if (_rootElement is not null)
					DetachRootHandlers();

				_rootElement = root;
				_rootElement.AddHandler(UIElement.PointerMovedEvent, _rootPointerMovedHandler, true);
				_rootElement.AddHandler(UIElement.PointerPressedEvent, _rootPointerPressedHandler, true);
				_rootElement.AddHandler(UIElement.PointerReleasedEvent, _rootPointerReleasedHandler, true);
				_rootElement.AddHandler(UIElement.KeyDownEvent, _rootKeyDownHandler, true);
			}

			private void DetachFromVisualTree()
			{
				_scrollViewer = null;

				if (_rootElement is null)
					return;

				DetachRootHandlers();
				_rootElement = null;
			}

			private void DetachRootHandlers()
			{
				_rootElement?.RemoveHandler(UIElement.PointerMovedEvent, _rootPointerMovedHandler);
				_rootElement?.RemoveHandler(UIElement.PointerPressedEvent, _rootPointerPressedHandler);
				_rootElement?.RemoveHandler(UIElement.PointerReleasedEvent, _rootPointerReleasedHandler);
				_rootElement?.RemoveHandler(UIElement.KeyDownEvent, _rootKeyDownHandler);
			}

			private void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
			{
				if (!_isEnabled)
					return;

				if (_isAutoScrollActive)
				{
					StopAutoScroll();
					e.Handled = true;
					return;
				}

				var point = e.GetCurrentPoint(_rootElement ?? _element);
				if (!point.Properties.IsMiddleButtonPressed)
					return;

				// Keep middle-click-to-open-folder-in-new-tab behavior unchanged.
				if (e.OriginalSource is FrameworkElement { DataContext: ListedItem item } && item.PrimaryItemAttribute == StorageItemTypes.Folder)
					return;

				_scrollViewer ??= _element.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer")
					?? _element.FindDescendant<ScrollViewer>();
				if (_scrollViewer is null || !CanScroll(_scrollViewer))
					return;

				_anchorPosition = point.Position;
				_currentPosition = _anchorPosition;
				_isAutoScrollActive = true;
				_activationPressTimestamp = point.Timestamp;
				_ignoreActivationMiddleRelease = true;
				_holdScrollDetected = false;
				_scrollTimer.Start();
				ApplyCursor(AutoScrollCursor, e.OriginalSource as UIElement);
				e.Handled = true;
			}

			private void RootElement_PointerMoved(object sender, PointerRoutedEventArgs e)
			{
				if (!_isAutoScrollActive)
					return;

				var point = e.GetCurrentPoint(_rootElement ?? _element);
				_currentPosition = point.Position;

				if (point.Properties.IsMiddleButtonPressed)
				{
					var deltaY = _currentPosition.Y - _anchorPosition.Y;
					var deltaX = _currentPosition.X - _anchorPosition.X;
					if (Math.Abs(deltaY) > DeadZone || Math.Abs(deltaX) > DeadZone)
						_holdScrollDetected = true;
				}

				ApplyCursor(AutoScrollCursor, e.OriginalSource as UIElement);
			}

			private void RootElement_PointerPressed(object sender, PointerRoutedEventArgs e)
			{
				if (!_isAutoScrollActive)
					return;

				var point = e.GetCurrentPoint(_rootElement ?? _element);

				// Ignore the same click that activated auto-scroll.
				if (point.Timestamp == _activationPressTimestamp)
				{
					return;
				}

				if (point.Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed)
				{
					StopAutoScroll();
					e.Handled = true;
					return;
				}

				// Any other pointer press exits auto-scroll mode.
				StopAutoScroll();
				e.Handled = true;
			}

			private void RootElement_PointerReleased(object sender, PointerRoutedEventArgs e)
			{
				if (!_isAutoScrollActive)
					return;

				var point = e.GetCurrentPoint(_rootElement ?? _element);
				if (point.Properties.PointerUpdateKind != PointerUpdateKind.MiddleButtonReleased)
					return;

				// Ignore the release from the original activation click.
				if (_ignoreActivationMiddleRelease)
				{
					_ignoreActivationMiddleRelease = false;

					// Hold-to-scroll: stop when the user releases the middle button after dragging.
					if (_holdScrollDetected)
					{
						StopAutoScroll();
						e.Handled = true;
					}

					return;
				}

				StopAutoScroll();
				e.Handled = true;
			}

			private void RootElement_KeyDown(object sender, KeyRoutedEventArgs e)
			{
				if (_isAutoScrollActive && e.Key == VirtualKey.Escape)
				{
					StopAutoScroll();
					e.Handled = true;
				}
			}

			private void ScrollTimer_Tick(DispatcherQueueTimer sender, object args)
			{
				if (!_isAutoScrollActive)
					return;

				_scrollViewer ??= _element.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer")
					?? _element.FindDescendant<ScrollViewer>();
				if (_scrollViewer is null)
					return;

				var deltaY = _currentPosition.Y - _anchorPosition.Y;
				var deltaX = _currentPosition.X - _anchorPosition.X;

				double? newHorizontalOffset = null;
				double? newVerticalOffset = null;

				if (_scrollViewer.ScrollableHeight > 0)
				{
					var velocityY = CalculateVelocity(deltaY);
					if (Math.Abs(velocityY) > 0)
					{
						var targetY = Math.Clamp(_scrollViewer.VerticalOffset + velocityY, 0, _scrollViewer.ScrollableHeight);
						if (!targetY.Equals(_scrollViewer.VerticalOffset))
							newVerticalOffset = targetY;
					}
				}

				if (_scrollViewer.ScrollableWidth > 0)
				{
					var velocityX = CalculateVelocity(deltaX);
					if (Math.Abs(velocityX) > 0)
					{
						var targetX = Math.Clamp(_scrollViewer.HorizontalOffset + velocityX, 0, _scrollViewer.ScrollableWidth);
						if (!targetX.Equals(_scrollViewer.HorizontalOffset))
							newHorizontalOffset = targetX;
					}
				}

				if (newHorizontalOffset is not null || newVerticalOffset is not null)
					_scrollViewer.ChangeView(newHorizontalOffset, newVerticalOffset, null, true);
			}

			private void StopAutoScroll()
			{
				var wasAutoScrollActive = _isAutoScrollActive;
				_isAutoScrollActive = false;
				_activationPressTimestamp = 0;
				_ignoreActivationMiddleRelease = false;
				_holdScrollDetected = false;
				if (_scrollTimer.IsRunning)
					_scrollTimer.Stop();

				if (wasAutoScrollActive)
					ResetCursor();
			}

			private void ApplyCursor(InputCursor cursor, UIElement? cursorTarget = null)
			{
				_rootElement ??= MainWindow.Instance?.Content as UIElement;

				_rootElement?.ChangeCursor(cursor);
				_element.ChangeCursor(cursor);
				_scrollViewer?.ChangeCursor(cursor);

				if (!ReferenceEquals(_lastCursorTarget, cursorTarget))
				{
					_lastCursorTarget?.ChangeCursor(DefaultCursor);
					_lastCursorTarget = cursorTarget;
				}

				_lastCursorTarget?.ChangeCursor(cursor);
			}

			private void ResetCursor()
			{
				_rootElement ??= MainWindow.Instance?.Content as UIElement;

				_rootElement?.ChangeCursor(DefaultCursor);
				_element.ChangeCursor(DefaultCursor);
				_scrollViewer?.ChangeCursor(DefaultCursor);
				_lastCursorTarget?.ChangeCursor(DefaultCursor);
				_lastCursorTarget = null;
			}

			private static bool CanScroll(ScrollViewer scrollViewer)
			{
				return scrollViewer.ScrollableHeight > 0 || scrollViewer.ScrollableWidth > 0;
			}

			private static double CalculateVelocity(double delta)
			{
				if (Math.Abs(delta) <= DeadZone)
					return 0;

				var adjustedDelta = delta - (Math.Sign(delta) * DeadZone);
				return Math.Clamp(adjustedDelta * SpeedFactor, -MaxSpeedPerTick, MaxSpeedPerTick);
			}
		}
	}
}
