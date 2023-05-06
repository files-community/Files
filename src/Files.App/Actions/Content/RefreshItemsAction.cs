// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class RefreshItemsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Refresh".GetLocalizedResource();
		public string Description { get; } = "RefreshItemsDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE72C");

		public HotKey HotKey { get; } = new(Keys.R, KeyModifiers.Ctrl);
		public HotKey SecondHotKey { get; } = new(Keys.F5);

		public bool IsExecutable => context.CanRefresh;

		public RefreshItemsAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			context.ShellPage?.Refresh_Click();
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
