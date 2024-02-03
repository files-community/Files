// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class UndoAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Undo".GetLocalizedResource();

		public string Description
			=> "UndoDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Z, KeyModifiers.Ctrl);

		public bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			ContentPageContext.PageType is not ContentPageTypes.SearchResults;

		public UndoAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return ContentPageContext.ShellPage.StorageHistoryHelpers.TryUndo();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
