// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using VirtualKey = Windows.System.VirtualKey;

namespace Files.App.Helpers
{
	/// <summary>
	/// Refreshes the drag caption when a modifier key changes while the cursor is not moving.
	/// </summary>
	public static class DragModifierKeysHelper
	{
		private const DragDropModifiers ModifierKeysMask = DragDropModifiers.Shift | DragDropModifiers.Control | DragDropModifiers.Alt;

		private static readonly DragEventHandler _dragOverHandler = new(Root_DragOver);

		private static DispatcherQueueTimer? _timer;
		private static DragDropModifiers _lastModifiers;
		private static DragDropModifiers? _lastRefreshedModifiers;

		public static void Attach(UIElement root)
		{
			_timer ??= CreateTimer(root.DispatcherQueue);
			root.AddHandler(UIElement.DragOverEvent, _dragOverHandler, true);
		}

		private static DispatcherQueueTimer CreateTimer(DispatcherQueue dispatcherQueue)
		{
			var timer = dispatcherQueue.CreateTimer();
			timer.Interval = TimeSpan.FromMilliseconds(100);
			timer.Tick += Timer_Tick;

			return timer;
		}

		private static void Root_DragOver(object sender, DragEventArgs e)
		{
			_lastModifiers = e.Modifiers & ModifierKeysMask;

			if (_timer is { IsRunning: false })
			{
				_lastRefreshedModifiers = null;
				_timer.Start();
			}
		}

		private static void Timer_Tick(DispatcherQueueTimer sender, object args)
		{
			// The drag ended
			if (!IsKeyDown(VirtualKey.LeftButton) && !IsKeyDown(VirtualKey.RightButton))
			{
				sender.Stop();
				return;
			}

			var modifiers = GetCurrentModifiers();
			if (modifiers == _lastModifiers || modifiers == _lastRefreshedModifiers)
				return;

			_lastRefreshedModifiers = modifiers;

			// WinUI only raises DragOver when the cursor moves
			RaiseMouseMove();
		}

		private static DragDropModifiers GetCurrentModifiers()
		{
			var modifiers = DragDropModifiers.None;

			if (IsKeyDown(VirtualKey.Shift))
				modifiers |= DragDropModifiers.Shift;
			if (IsKeyDown(VirtualKey.Control))
				modifiers |= DragDropModifiers.Control;
			if (IsKeyDown(VirtualKey.Menu))
				modifiers |= DragDropModifiers.Alt;

			return modifiers;
		}

		private static bool IsKeyDown(VirtualKey key)
		{
			return PInvoke.GetAsyncKeyState((int)key) < 0;
		}

		private static void RaiseMouseMove()
		{
			// Moves the cursor by one pixel and back
			ReadOnlySpan<INPUT> inputs =
			[
				CreateMouseMove(1),
				CreateMouseMove(-1),
			];

			PInvoke.SendInput(inputs, Unsafe.SizeOf<INPUT>());
		}

		private static INPUT CreateMouseMove(int dx)
		{
			var input = new INPUT { type = INPUT_TYPE.INPUT_MOUSE };
			input.Anonymous.mi.dx = dx;
			input.Anonymous.mi.dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE;

			return input;
		}
	}
}
