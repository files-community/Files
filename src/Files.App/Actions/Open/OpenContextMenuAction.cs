using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.UserControls;
using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenContextMenuAction : ObservableObject, IAction
	{
		private FrameworkElement? focused;

		public string Label { get; } = "OpenContextMenu".GetLocalizedResource();
		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.Enter, VirtualKeyModifiers.Control);

		public bool IsExecutable => focused is not null;

		public OpenContextMenuAction()
		{
			FocusManager.GotFocus += FocusManager_GotFocus;
			FocusManager.LosingFocus += FocusManager_LosingFocus;
		}
		public async Task ExecuteAsync()
		{
			if (focused is null)
				return;

			var point = focused.GetVisualInternal().CenterPoint;
			var position = new Point(point.X + 16, point.Y + 16);

			if (focused.DataContext is INavigationControlItem)
			{
				var sidebar = focused.FindAscendant<SidebarControl>();
				if (sidebar is not null)
				{
					await sidebar.OpenContextMenuAsync(focused, position);
					return;
				}
			}
			if (focused.DataContext is WidgetCardItem)
			{
				var widget = focused.FindAscendant<HomePageWidget>();
				if (widget is not null)
				{
					await widget.OpenContextMenuAsync(focused, position);
					return;
				}
			}

			if (focused.ContextFlyout is FlyoutBase menu && !menu.IsOpen)
			{
				var options = new FlyoutShowOptions { Position = position };
				menu.ShowAt(focused, options);
			}
		}

		private void FocusManager_GotFocus(object? sender, FocusManagerGotFocusEventArgs e)
		{
			if (e.NewFocusedElement is FrameworkElement newFocused)
			{
				if (newFocused.ContextFlyout is not null || newFocused.DataContext is INavigationControlItem or WidgetCardItem)
				{
					focused = newFocused;
					OnPropertyChanged(nameof(IsExecutable));
				}
			}
		}
		private void FocusManager_LosingFocus(object? sender, LosingFocusEventArgs e)
		{
			if (focused is not null)
			{
				focused = null;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
	}
}
