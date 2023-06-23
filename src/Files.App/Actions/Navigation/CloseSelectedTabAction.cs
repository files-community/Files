// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CloseSelectedTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context;

		public string Label
			=> "CloseTab".GetLocalizedResource();

		public string Description
			=> "CloseSelectedTabDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.W, KeyModifiers.Ctrl);

		public HotKey SecondHotKey
			=> new(Keys.F4, KeyModifiers.Ctrl);

		public RichGlyph Glyph
			=> new();

		public bool IsExecutable =>
			context.Control is not null &&
			context.TabCount > 0 &&
			context.CurrentTabItem is not null;

		public CloseSelectedTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.Control!.CloseTab(context.CurrentTabItem);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.CurrentTabItem):
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
