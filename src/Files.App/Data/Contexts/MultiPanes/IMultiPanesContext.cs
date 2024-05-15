// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	/// <summary>
	/// Represents context for <see cref="PaneHolderPage"/>, which manages multiple panes.
	/// </summary>
	public interface IMultiPanesContext
	{
		/// <summary>
		/// Gets invoked when active pane is changing.
		/// </summary>
		event EventHandler? ActivePaneChanging;

		/// <summary>
		/// Gets invoked when active pane is changed.
		/// </summary>
		event EventHandler? ActivePaneChanged;

		/// <summary>
		/// Gets active pane.
		/// </summary>
		IShellPage? ActivePane { get; }

		/// <summary>
		/// Gets active pane or column.
		/// </summary>
		IShellPage? ActivePaneOrColumn { get; }
	}
}
