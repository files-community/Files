// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Contexts
{
	public interface IPageContext
	{
		event EventHandler? Changing;
		event EventHandler? Changed;

		IShellPage? Pane { get; }
		IShellPage? PaneOrColumn { get; }
	}
}
