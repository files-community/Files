using Files.App.UserControls.MultitaskingControl;
using Files.App.Views;
using System;
using System.ComponentModel;

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
			PaneHolderPage.CurrentInstanceChanged += PaneHolderPage_CurrentInstanceChanged;
		}

		private void PaneHolderPage_CurrentInstanceChanged(object? sender, PaneHolderPage? newPage)
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

			Update();
		}

		private void Page_ContentChanged(object? sender, TabItemArguments e) => Update();

		private void Page_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IPaneHolder.ActivePane):
				case nameof(IPaneHolder.ActivePaneOrColumn):
					Update();
					break;
			}
		}

		private void Update()
		{
			Changing?.Invoke(this, EventArgs.Empty);

			pane = page?.ActivePane;
			paneOrColumn = page?.ActivePaneOrColumn;

			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
