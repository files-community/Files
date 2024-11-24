using Microsoft.UI.Xaml;
using System.Globalization;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace Files.App.Services.Content
{
	internal sealed class RealTimeLayoutService : IRealTimeLayoutService
	{
		private readonly List<(WeakReference<object> Reference, Action Callback)> _callbacks = [];
		public static FlowDirection FlowDirection => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

		public void UpdateCulture(CultureInfo culture)
		{
			FlowDirection tmp = FlowDirection;

			CultureInfo.CurrentUICulture = culture;

			if (tmp != FlowDirection)
				InvokeCallbacks();
		}

		public void AddCallback(object target, Action callback)
		{
			var weakReference = new WeakReference<object>(target);
			_callbacks.Add((weakReference, callback));

			if (target is Window window)
				window.Closed += (sender, args) => RemoveCallback(target);

			if (target is FrameworkElement element)
				element.Unloaded += (sender, args) => RemoveCallback(target);
		}

		public bool UpdateTitleBar(Window window)
		{
			try
			{
				var hwnd = new HWND(WindowNative.GetWindowHandle(window));
				var exStyle = PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

				exStyle = FlowDirection is FlowDirection.RightToLeft
					? new((uint)exStyle | (uint)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL) // Set RTL layout
					: new((uint)exStyle.ToInt64() & ~(uint)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL); // Set LTR layout

				if (PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle) == 0)
					return false;
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		public void UpdateContent(Window window)
		{
			if (window.Content is FrameworkElement frameworkElement)
				frameworkElement.FlowDirection = FlowDirection;
		}

		public void UpdateContent(FrameworkElement frameworkElement)
		{
			frameworkElement.FlowDirection = FlowDirection;
		}

		private void RemoveCallback(object target)
		{
			_callbacks.RemoveAll(item =>
				item.Reference.TryGetTarget(out var targetObject) && targetObject == target);
		}

		private void InvokeCallbacks()
		{
			_callbacks.Where(item =>
				item.Reference.TryGetTarget(out var targetObject) && targetObject != null)
				.Select(item => item.Callback).ForEach(callback => callback());
			_callbacks.RemoveAll(item => !item.Reference.TryGetTarget(out _));
		}
	}
}
