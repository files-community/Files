// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenPropertiesAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public string Label
			=> "OpenProperties".GetLocalizedResource();

		public string Description
			=> "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconProperties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.Menu);

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenPropertiesAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			FilePropertiesHelpers.OpenPropertiesWindow(ContentPageContext.ShellPage!);

			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.PageType is not ContentPageTypes.Home &&
				!(ContentPageContext.PageType is ContentPageTypes.SearchResults &&
				!ContentPageContext.HasSelection);

			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked;

			return executableInDisplayPage || executableInHomePage;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
