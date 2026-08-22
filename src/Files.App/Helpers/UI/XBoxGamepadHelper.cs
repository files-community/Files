using System;
using System.Runtime.InteropServices;
using Windows.Gaming.Input;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Helpers
{
	public sealed class XBoxGamepadHelper
	{
		private readonly DispatcherQueueTimer _timer;
		private readonly float _deadzone = 0.15f;
		private readonly float _sensitivity = 12f;
		private float _leftTriggerThreshold = 0.5f;
		private bool _wasLeftTriggerPressed;
		private bool _wasRightTriggerPressed;
		private bool _isDragging;

		private bool _wasAPressed;
		private bool _wasXPressed;
		private bool _wasBPressed;

		private bool _wasDPadDownPressed;
		private bool _wasDPadUpPressed;
		private bool _wasDPadLeftPressed;
		private bool _wasDPadRightPressed;

		public XBoxGamepadHelper(DispatcherQueue dispatcherQueue)
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

			var dx = (int)Math.Round(leftThumbX * _sensitivity);
			var dy = (int)Math.Round(-leftThumbY * _sensitivity);
			if (dx != 0 || dy != 0)
				SimulateMouseMove(dx, dy);

			// Left & Right Triggers
			var leftTriggerValue = reading.LeftTrigger;
			var isLeftTriggerPressed = leftTriggerValue > _leftTriggerThreshold;
			if (isLeftTriggerPressed && !_wasLeftTriggerPressed)
			{
				_isDragging = true;
				SimulateMouseDownOnly();
			}
			_wasLeftTriggerPressed = isLeftTriggerPressed;
			if (!isLeftTriggerPressed && _isDragging)
			{
				_isDragging = false;
				SimulateMouseUpOnly();
			}

			var rightTriggerValue = reading.RightTrigger;
			var isRightTriggerPressed = rightTriggerValue > _leftTriggerThreshold;

			if (isRightTriggerPressed && !_wasRightTriggerPressed)
			{
				SimulateRightMouseClick();
			}
			_wasRightTriggerPressed = isRightTriggerPressed;

			// Gamepad Buttons A, X, B
			var isAPressed = (reading.Buttons & GamepadButtons.A) == GamepadButtons.A;
			if (isAPressed && !_wasAPressed)
			{
				SimulateLeftMouseClick();
			}
			_wasAPressed = isAPressed;

			var isXPressed = (reading.Buttons & GamepadButtons.X) == GamepadButtons.X;
			if (isXPressed && !_wasXPressed)
			{
				SimulateRightMouseClick();
			}
			_wasXPressed = isXPressed;

			var isBPressed = (reading.Buttons & GamepadButtons.B) == GamepadButtons.B;
			if (isBPressed && !_wasBPressed)
			{
				HandleBackOrCancel();
			}
			_wasBPressed = isBPressed;


			var isDDownPressed = (reading.Buttons & GamepadButtons.DPadDown) == GamepadButtons.DPadDown;
			if (isDDownPressed && !_wasDPadDownPressed)
				SimulateKeyPress(Windows.System.VirtualKey.Down);
			_wasDPadDownPressed = isDDownPressed;

			var isDUpPressed = (reading.Buttons & GamepadButtons.DPadUp) == GamepadButtons.DPadUp;
			if (isDUpPressed && !_wasDPadUpPressed)
				SimulateKeyPress(Windows.System.VirtualKey.Up);
			_wasDPadUpPressed = isDUpPressed;

			var isDLeftPressed = (reading.Buttons & GamepadButtons.DPadLeft) == GamepadButtons.DPadLeft;
			if (isDLeftPressed && !_wasDPadLeftPressed)
				SimulateKeyPress(Windows.System.VirtualKey.Left);
			_wasDPadLeftPressed = isDLeftPressed;

			var isDRightPressed = (reading.Buttons & GamepadButtons.DPadRight) == GamepadButtons.DPadRight;
			if (isDRightPressed && !_wasDPadRightPressed)
				SimulateKeyPress(Windows.System.VirtualKey.Right);
			_wasDPadRightPressed = isDRightPressed;

			var rightThumbY = reading.RightThumbstickY;
			if (Math.Abs(rightThumbY) > _deadzone)
			{
				var wheelDelta = (int)Math.Round(-rightThumbY * 120f);
				if (wheelDelta != 0)
					SimulateMouseWheel(wheelDelta);
			}
		}

		private static void HandleBackOrCancel()
		{
			if (MainWindow.Instance.Content is not Microsoft.UI.Xaml.Controls.Frame rootFrame)
				return;

			var currentPage = rootFrame.Content as Microsoft.UI.Xaml.Controls.Page;
			if (currentPage is null)
				return;

			var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(rootFrame.XamlRoot);
			if (openPopups.Count > 0)
			{
				SimulateKeyPress(Windows.System.VirtualKey.Escape);
				return;
			}

			var pageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			var shell = pageContext.ShellPage;

			if (shell is null)
				return;

			if (shell.CanNavigateBackward)
			{
				shell.Back_Click();
				return;
			}

			SimulateKeyPress(Windows.System.VirtualKey.Escape);
		}

		// Key Input Simulation
		private static void SimulateKeyPress(Windows.System.VirtualKey key)
		{
			var vk = (VIRTUAL_KEY)(ushort)key;

			var down = new INPUT
			{
				type = INPUT_TYPE.INPUT_KEYBOARD,
				Anonymous = new INPUT._Anonymous_e__Union
				{
					ki = new KEYBDINPUT { wVk = vk }
				}
			};
			PInvoke.SendInput(new ReadOnlySpan<INPUT>(ref down), Marshal.SizeOf<INPUT>());

			var up = new INPUT
			{
				type = INPUT_TYPE.INPUT_KEYBOARD,
				Anonymous = new INPUT._Anonymous_e__Union
				{
					ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP }
				}
			};
			PInvoke.SendInput(new ReadOnlySpan<INPUT>(ref up), Marshal.SizeOf<INPUT>());
		}


		// Mouse Action Simulation
		private static void SimulateMouseMove(int dx, int dy)
		{
			var input = new INPUT
			{
				type = INPUT_TYPE.INPUT_MOUSE,
				Anonymous = new INPUT._Anonymous_e__Union
				{
					mi = new MOUSEINPUT
					{
						dx = dx,
						dy = dy,
						dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE
					}
				}
			};

			PInvoke.SendInput(new ReadOnlySpan<INPUT>(ref input), Marshal.SizeOf<INPUT>());
		}


		private static void SimulateMouseEvent(MOUSE_EVENT_FLAGS flags, uint mouseData = 0)
		{
			var input = new INPUT
			{
				type = INPUT_TYPE.INPUT_MOUSE,
				Anonymous = new INPUT._Anonymous_e__Union
				{
					mi = new MOUSEINPUT
					{
						dwFlags = flags,
						mouseData = mouseData,
					}
				}
			};

			PInvoke.SendInput(new ReadOnlySpan<INPUT>(ref input), Marshal.SizeOf<INPUT>());
		}

		private static void SimulateMouseDownOnly() =>
			SimulateMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN);

		private static void SimulateMouseUpOnly() =>
			SimulateMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP);

		private static void SimulateLeftMouseClick()
		{
			SimulateMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN);
			SimulateMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP);
		}

		private static void SimulateRightMouseClick()
		{
			SimulateMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN);
			SimulateMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP);
		}

		private static void SimulateMouseWheel(int delta) =>
			SimulateMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_WHEEL, unchecked((uint)delta));
	}
}
