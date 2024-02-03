// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CutItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Cut".GetLocalizedResource();

		public string Description
			=> "CutItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconCut");

		public HotKey HotKey
			=> new(Keys.X, KeyModifiers.Ctrl);

		public bool IsExecutable
			=> ContentPageContext.HasSelection;

		public CutItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return ContentPageContext.ShellPage is not null
				? UIFilesystemHelpers.CutItemAsync(ContentPageContext.ShellPage)
				: Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
