using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.UserControls;
using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenContextMenuAction : IAction
	{
		public string Label { get; } = "OpenContextMenu".GetLocalizedResource();
		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.Enter, VirtualKeyModifiers.Control);

		public bool IsExecutable
		{
			get
			{
				if (App.LastOpenedFlyout is CommandBarFlyout { IsOpen: true })
					return false;

				var element = GetFocusedElement();
				if (element is null)
					return false;

				return element.ContextFlyout is FlyoutBase { IsOpen: false }
				|| element.DataContext is INavigationControlItem or WidgetCardItem;
			}
		}

		public async Task ExecuteAsync()
		{
			var element = GetFocusedElement();
			if (element is null)
				return;

			var point = element.GetVisualInternal().CenterPoint;
			var position = new Point(point.X + 16, point.Y + 16);

			if (element.DataContext is INavigationControlItem)
			{
				var sidebar = element.FindAscendant<SidebarControl>();
				if (sidebar is not null)
				{
					await sidebar.OpenContextMenuAsync(element, position);
				}
			}
			else if (element.DataContext is WidgetCardItem)
			{
				var widget = element.FindAscendant<HomePageWidget>();
				if (widget is not null)
				{
					await widget.OpenContextMenuAsync(element, position);
				}
			}
			else if (element.ContextFlyout is FlyoutBase{IsOpen: false} flyout)
			{
				var options = new FlyoutShowOptions { Position = position };
				flyout.ShowAt(element, options);
			}
		}

		private static FrameworkElement? GetFocusedElement()
		{
			return FocusManager.GetFocusedElement(App.Window.Content.XamlRoot) as FrameworkElement;
		}
	}
}
