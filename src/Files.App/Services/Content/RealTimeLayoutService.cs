// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Globalization;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace Files.App.Services.Content
{
	/// <summary>
	/// Provides a service to manage real-time layout updates, including content layout and title bar updates,
	/// while supporting flow direction based on the current culture.
	/// </summary>
	internal sealed class RealTimeLayoutService : IRealTimeLayoutService
	{
		/// <summary>
		/// List of weak references to target objects and their associated callbacks.
		/// </summary>
		private readonly List<(WeakReference<object> Reference, Action Callback)> _callbacks = [];

		/// <summary>
		/// Gets the current flow direction based on the current UI culture (RightToLeft or LeftToRight).
		/// </summary>
		public FlowDirection FlowDirection => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

		/// <summary>
		/// Updates the culture for the layout service and invokes the necessary callbacks if the flow direction changes.
		/// </summary>
		/// <param name="culture">The new culture information to apply.</param>
		public void UpdateCulture(CultureInfo culture)
		{
			FlowDirection tmp = FlowDirection;

			CultureInfo.DefaultThreadCurrentUICulture = culture;
			CultureInfo.CurrentUICulture = culture;

			if (tmp != FlowDirection)
				InvokeCallbacks();
		}

		/// <summary>
		/// Registers a callback to be invoked when the layout needs to be updated for the specified target.
		/// </summary>
		/// <param name="target">The target object to associate with the callback.</param>
		/// <param name="callback">The callback to invoke when the layout is updated.</param>
		public void AddCallback(object target, Action callback)
		{
			var weakReference = new WeakReference<object>(target);
			_callbacks.Add((weakReference, callback));

			if (target is Window window)
				window.Closed += (sender, args) => RemoveCallback(target);

			if (target is FrameworkElement element)
				element.Unloaded += (sender, args) => RemoveCallback(target);
		}

		/// <summary>
		/// Updates the title bar layout of the specified window based on the current flow direction.
		/// </summary>
		/// <param name="window">The window whose title bar layout needs updating.</param>
		/// <returns>True if the title bar layout was successfully updated; otherwise, false.</returns>
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

		/// <summary>
		/// Updates the content layout of the specified window to match the current flow direction.
		/// </summary>
		/// <param name="window">The window whose content layout needs updating.</param>
		public void UpdateContent(Window window)
		{
			if (window.Content is FrameworkElement frameworkElement)
				frameworkElement.FlowDirection = FlowDirection;
		}

		/// <summary>
		/// Updates the content layout of the specified framework element to match the current flow direction.
		/// </summary>
		/// <param name="frameworkElement">The framework element whose content layout needs updating.</param>
		public void UpdateContent(FrameworkElement frameworkElement)
		{
			frameworkElement.FlowDirection = FlowDirection;
		}

		/// <summary>
		/// Removes the callback associated with the specified target.
		/// </summary>
		/// <param name="target">The target object whose callback needs to be removed.</param>
		private void RemoveCallback(object target)
		{
			_callbacks.RemoveAll(item =>
				item.Reference.TryGetTarget(out var targetObject) && targetObject == target);
		}

		/// <summary>
		/// Invokes all registered callbacks for targets that are still valid.
		/// </summary>
		private void InvokeCallbacks()
		{
			_callbacks.Where(item =>
				item.Reference.TryGetTarget(out var targetObject) && targetObject != null)
				.Select(item => item.Callback).ForEach(callback => callback());
			_callbacks.RemoveAll(item => !item.Reference.TryGetTarget(out _));
		}
	}
}
