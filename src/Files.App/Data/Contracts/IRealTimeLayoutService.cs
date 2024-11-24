// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Globalization;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides an interface for managing real-time layout updates and related operations.
	/// </summary>
	public interface IRealTimeLayoutService
	{
		/// <summary>
		/// Gets the current flow direction for layout (e.g., LeftToRight or RightToLeft).
		/// </summary>
		FlowDirection FlowDirection { get; }

		/// <summary>
		/// Adds a callback to be executed when a specific target requires updates.
		/// </summary>
		/// <param name="target">The target object for which the callback is registered.</param>
		/// <param name="callback">The action to execute during updates.</param>
		void AddCallback(object target, Action callback);

		/// <summary>
		/// Updates the content layout of the specified framework element.
		/// </summary>
		/// <param name="frameworkElement">The framework element to update.</param>
		void UpdateContent(FrameworkElement frameworkElement);

		/// <summary>
		/// Updates the content layout of the specified window.
		/// </summary>
		/// <param name="window">The window whose content layout needs updating.</param>
		void UpdateContent(Window window);

		/// <summary>
		/// Updates the culture settings for the layout.
		/// </summary>
		/// <param name="culture">The culture information to apply.</param>
		void UpdateCulture(CultureInfo culture);

		/// <summary>
		/// Updates the title bar of the specified window.
		/// </summary>
		/// <param name="window">The window whose title bar needs updating.</param>
		/// <returns>True if the title bar was successfully updated; otherwise, false.</returns>
		bool UpdateTitleBar(Window window);
	}
}
