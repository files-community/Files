// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RefreshItemsAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Refresh".GetLocalizedResource();

		public string Description
			=> "RefreshItemsDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE72C");

		public HotKey HotKey
			=> new(Keys.R, KeyModifiers.Ctrl);

		public HotKey SecondHotKey
			=> new(Keys.F5);

		public bool IsExecutable
			=> ContentPageContext.CanRefresh;

		public RefreshItemsAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is null)
				return;

			await ContentPageContext.ShellPage.Refresh_Click();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanRefresh):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
