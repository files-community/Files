// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class NavigateBackAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Back".GetLocalizedResource();

		public string Description
			=> "NavigateBackDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Left, KeyModifiers.Menu);

		public HotKey SecondHotKey
			=> new(Keys.Back);

		public HotKey ThirdHotKey
			=> new(Keys.Mouse4);

		public HotKey MediaHotKey
			=> new(Keys.GoBack, false);

		public RichGlyph Glyph
			=> new("\uE72B");

		public bool IsExecutable
			=> ContentPageContext.CanGoBack;

		public NavigateBackAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			ContentPageContext.ShellPage!.Back_Click();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanGoBack):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
