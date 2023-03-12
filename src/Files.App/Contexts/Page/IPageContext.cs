using System;

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
