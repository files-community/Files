// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="IMultiPanesContext"/>
	internal sealed class MultiPanesContext : IMultiPanesContext
	{
		private MainPanesPage? _mainPanesPage;

		private IShellPage? _ActivePane;
		/// <inheritdoc/>
		public IShellPage? ActivePane
			=> _ActivePane;

		private IShellPage? _ActivePaneOrColumn;
		/// <inheritdoc/>
		public IShellPage? ActivePaneOrColumn
			=> _ActivePaneOrColumn;

		/// <inheritdoc/>
		public event EventHandler? ActivePane_Changing;

		/// <inheritdoc/>
		public event EventHandler? ActivePane_Changed;

		/// <summary>
		/// Initializes an instance of <see cref="MultiPanesContext"/>.
		/// </summary>
		public MultiPanesContext()
		{
			MainPanesPage.CurrentInstanceChanged += Page_CurrentInstanceChanged;
		}

		private void Page_CurrentInstanceChanged(object? sender, MainPanesPage? modifiedPage)
		{
			if (_mainPanesPage is not null && !_mainPanesPage.IsCurrentInstance)
			{
				UpdatePage(null);
			}
			else if (modifiedPage is not null && modifiedPage.IsCurrentInstance)
			{
				UpdatePage(modifiedPage);
			}
		}

		private void Page_ContentChanged(object? sender, TabBarItemParameter e)
		{
			UpdateContent();
		}

		private void Page_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IPanesPage.ActivePaneOrColumn):
					UpdateContent();
					break;
			}
		}

		private void UpdatePage(MainPanesPage? newPage)
		{
			if (Equals(_mainPanesPage, newPage))
				return;

			if (_mainPanesPage is not null)
			{
				_mainPanesPage.ContentChanged -= Page_ContentChanged;
				_mainPanesPage.PropertyChanged -= Page_PropertyChanged;
			}

			_mainPanesPage = newPage;

			if (_mainPanesPage is not null)
			{
				_mainPanesPage.ContentChanged += Page_ContentChanged;
				_mainPanesPage.PropertyChanged += Page_PropertyChanged;
			}

			UpdateContent();
		}

		private void UpdateContent()
		{
			if (_ActivePane == _mainPanesPage?.ActivePane &&
				_ActivePaneOrColumn == _mainPanesPage?.ActivePaneOrColumn)
				return;

			ActivePane_Changing?.Invoke(this, EventArgs.Empty);

			_ActivePane = _mainPanesPage?.ActivePane;
			_ActivePaneOrColumn = _mainPanesPage?.ActivePaneOrColumn;

			ActivePane_Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
