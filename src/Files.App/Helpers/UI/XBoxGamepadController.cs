using System;
using System.Runtime.InteropServices;
using Windows.Gaming.Input;
using Windows.Win32;
using Microsoft.UI.Dispatching;

namespace Files.App.Helpers
{
	public sealed class XBoxGamepadController
	{
		[DllImport("user32.dll")]
		private static extern bool SetCursorPos(int x, int y);

		private readonly DispatcherQueueTimer _timer;
		private readonly float _deadzone = 0.15f;
		private readonly float _sensitivity = 12f;

		public XBoxGamepadController(DispatcherQueue dispatcherQueue)
		{
			_timer = dispatcherQueue.CreateTimer();
			_timer.Interval = TimeSpan.FromMilliseconds(16);
			_timer.Tick += OnTick;
			_timer.Start();
		}

		private void OnTick(object? sender, object e)
		{
			var gamepads = Gamepad.Gamepads;
			if (gamepads.Count == 0)
				return;

			var gamepad = gamepads[0];
			var reading = gamepad.GetCurrentReading();

			var leftThumbX = reading.LeftThumbstickX;
			var leftThumbY = reading.LeftThumbstickY;

			if (Math.Abs(leftThumbX) < _deadzone) leftThumbX = 0;
			if (Math.Abs(leftThumbY) < _deadzone) leftThumbY = 0;

			PInvoke.GetCursorPos(out var cursorPos);
			var dx = (int)Math.Round(leftThumbX * _sensitivity);
			var dy = (int)Math.Round(-leftThumbY * _sensitivity);

			SetCursorPos(cursorPos.X + dx, cursorPos.Y + dy);
		}
	}
}
