using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Commands;
using Files.App.Extensions;
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

		public bool IsExecutable => !(focused?.ContextFlyout?.IsOpen ?? true);

		public OpenContextMenuAction()
		{
			FocusManager.GotFocus += FocusManager_GotFocus;
			FocusManager.LosingFocus += FocusManager_LosingFocus;
		}

		public Task ExecuteAsync()
		{
			if (focused is null)
				return Task.CompletedTask;

			if (focused.ContextFlyout is FlyoutBase menu && !menu.IsOpen)
			{
				var point = focused.GetVisualInternal().CenterPoint;
				var position = new Point(point.X + 16, point.Y + 16);
				var options = new FlyoutShowOptions { Position = position };

				menu.ShowAt(focused, options);
			}

			return Task.CompletedTask;
		}

		private void FocusManager_GotFocus(object? sender, FocusManagerGotFocusEventArgs e)
		{
			if (e.NewFocusedElement is FrameworkElement newFocused && newFocused.ContextFlyout is not null)
			{
				focused = newFocused;
				newFocused.ContextFlyout.Opening += ContextFlyout_Opening;
				newFocused.ContextFlyout.Closed += ContextFlyout_Closed;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
		private void FocusManager_LosingFocus(object? sender, LosingFocusEventArgs e)
		{
			if (focused is not null)
			{
				focused.ContextFlyout.Closed -= ContextFlyout_Closed;
				focused.ContextFlyout.Opening -= ContextFlyout_Opening;
				focused = null;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}

		private void ContextFlyout_Opening(object? sender, object e) => OnPropertyChanged(nameof(IsExecutable));
		private void ContextFlyout_Closed(object? sender, object e) => OnPropertyChanged(nameof(IsExecutable));
	}
}
