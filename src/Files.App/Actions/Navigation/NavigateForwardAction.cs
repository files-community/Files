// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class NavigateForwardAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Forward".GetLocalizedResource();

		public string Description
			=> "NavigateForwardDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Right, KeyModifiers.Menu);

		public HotKey SecondHotKey
			=> new(Keys.Mouse5);

		public HotKey MediaHotKey
			=> new(Keys.GoForward, false);

		public RichGlyph Glyph
			=> new("\uE72A");

		public bool IsExecutable
			=> context.CanGoForward;

		public NavigateForwardAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.Forward_Click();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanGoForward):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
