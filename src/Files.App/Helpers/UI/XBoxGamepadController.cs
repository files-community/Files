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

		[DllImport("user32.dll")]
		private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

		[DllImport("user32.dll")]
		private static extern uint SendInput(uint nInputs, ref KEYINPUT pInputs, int cbSize);


		[StructLayout(LayoutKind.Sequential)]
		private struct KEYINPUT
		{
			public uint type;
			public KEYBDINPUT ki;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct KEYBDINPUT
		{
			public ushort wVk;
			public ushort wScan;
			public uint dwFlags;
			public long time;
			public nint dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct INPUT
		{
			public uint type;
			public MOUSEINPUT mi;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MOUSEINPUT
		{
			public int dx;
			public int dy;
			public uint mouseData;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		private const uint INPUT_MOUSE = 0;
		private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
		private const uint MOUSEEVENTF_LEFTUP = 0x0004;
		private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
		private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
		private const uint MOUSEEVENTF_WHEEL = 0x0800;

		private const uint INPUT_KEYBOARD = 1;
		private const uint KEYEVENTF_KEYDOWN = 0x0000;
		private const uint KEYEVENTF_KEYUP = 0x0002;


		private readonly DispatcherQueueTimer _timer;
		private readonly float _deadzone = 0.15f;
		private readonly float _sensitivity = 12f;

		private bool _wasAPressed;
		private bool _wasXPressed;
		private bool _wasBPressed;

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

			var rightThumbY = reading.RightThumbstickY;
			if (Math.Abs(rightThumbY) > _deadzone)
			{
				var wheelDelta = (int)Math.Round(-rightThumbY * 120f);
				if (wheelDelta != 0)
				{
					SimulateMouseWheel(wheelDelta);
				}
			}
		}

		private static void HandleBackOrCancel()
		{
			if (MainWindow.Instance.Content is not Microsoft.UI.Xaml.Controls.Frame rootFrame)
				return;

			var currentPage = rootFrame.Content as Microsoft.UI.Xaml.Controls.Page;
			if (currentPage is null)
				return;
			
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
			var down = new KEYINPUT
			{
				type = INPUT_KEYBOARD,
				ki = new KEYBDINPUT
				{
					wVk = (ushort)key,
					dwFlags = KEYEVENTF_KEYDOWN
				}
			};

			SendInput(1, ref down, Marshal.SizeOf<INPUT>());

			var up = new KEYINPUT
			{
				type = INPUT_KEYBOARD,
				ki = new KEYBDINPUT
				{
					wVk = (ushort)key,
					dwFlags = KEYEVENTF_KEYUP
				}
			};

			SendInput(1, ref up, Marshal.SizeOf<INPUT>());
		}


		// Mouse Action Simulation
		private static void SimulateLeftMouseClick()
		{
			var inputDown = new INPUT
			{
				type = INPUT_MOUSE,
				mi = new MOUSEINPUT
				{
					dwFlags = MOUSEEVENTF_LEFTDOWN
				}
			};

			SendInput(1, ref inputDown, Marshal.SizeOf<INPUT>());
			inputDown.mi.dwFlags = MOUSEEVENTF_LEFTUP;
			SendInput(1, ref inputDown, Marshal.SizeOf<INPUT>());

		}

		private static void SimulateRightMouseClick()
		{
			var inputDown = new INPUT
			{
				type = INPUT_MOUSE,
				mi = new MOUSEINPUT
				{
					dwFlags = MOUSEEVENTF_RIGHTDOWN
				}
			};

			SendInput(1, ref inputDown, Marshal.SizeOf<INPUT>());
			inputDown.mi.dwFlags = MOUSEEVENTF_RIGHTUP;
			SendInput(1, ref inputDown, Marshal.SizeOf<INPUT>());
		}

		private static void SimulateMouseWheel(int delta)
        {
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    mouseData = unchecked((uint)(delta)),
                    dwFlags = MOUSEEVENTF_WHEEL
                }
            };

            SendInput(1, ref input, Marshal.SizeOf<INPUT>());
        }
	}
}
