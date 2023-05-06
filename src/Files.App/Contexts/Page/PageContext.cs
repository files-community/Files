// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.MultitaskingControl;
using Files.App.Views;

namespace Files.App.Contexts
{
	internal class PageContext : IPageContext
	{
		public event EventHandler? Changing;
		public event EventHandler? Changed;

		private PaneHolderPage? page;

		private IShellPage? pane;
		public IShellPage? Pane => pane;

		private IShellPage? paneOrColumn;
		public IShellPage? PaneOrColumn => paneOrColumn;

		public PageContext()
		{
			PaneHolderPage.CurrentInstanceChanged += Page_CurrentInstanceChanged;
		}

		private void Page_CurrentInstanceChanged(object? sender, PaneHolderPage? modifiedPage)
		{
			if (page is not null && !page.IsCurrentInstance)
			{
				UpdatePage(null);
			}
			else if (modifiedPage is not null && modifiedPage.IsCurrentInstance)
			{
				UpdatePage(modifiedPage);
			}
		}

		private void Page_ContentChanged(object? sender, TabItemArguments e)
		{
			UpdateContent();
		}

		private void Page_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IPaneHolder.ActivePaneOrColumn))
			{
				UpdateContent();
			}
		}

		private void UpdatePage(PaneHolderPage? newPage)
		{
			if (Equals(page, newPage))
				return;

			if (page is not null)
			{
				page.ContentChanged -= Page_ContentChanged;
				page.PropertyChanged -= Page_PropertyChanged;
			}

			page = newPage;

			if (page is not null)
			{
				page.ContentChanged += Page_ContentChanged;
				page.PropertyChanged += Page_PropertyChanged;
			}

			UpdateContent();
		}

		private void UpdateContent()
		{
			Changing?.Invoke(this, EventArgs.Empty);

			pane = page?.ActivePane;
			paneOrColumn = page?.ActivePaneOrColumn;

			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
