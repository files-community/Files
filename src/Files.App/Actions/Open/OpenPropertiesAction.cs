// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenPropertiesAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "OpenProperties".GetLocalizedResource();

		public string Description
			=> "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconProperties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.Menu);

		public bool IsExecutable =>
			ContentPageContext.PageType is not ContentPageTypes.Home &&
			!(ContentPageContext.PageType is ContentPageTypes.SearchResults && 
			!ContentPageContext.HasSelection);

		public OpenPropertiesAction()
		{
			ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var page = ContentPageContext.ShellPage?.SlimContentPage;

			if (page?.ItemContextMenuFlyout.IsOpen ?? false)
				page.ItemContextMenuFlyout.Closed += OpenPropertiesFromItemContextMenuFlyout;
			else if (page?.BaseContextMenuFlyout.IsOpen ?? false)
				page.BaseContextMenuFlyout.Closed += OpenPropertiesFromBaseContextMenuFlyout;
			else
				FilePropertiesHelpers.OpenPropertiesWindow(ContentPageContext.ShellPage!);

			return Task.CompletedTask;
		}

		private void OpenPropertiesFromItemContextMenuFlyout(object? _, object e)
		{
			var page = ContentPageContext.ShellPage?.SlimContentPage;
			if (page is not null)
				page.ItemContextMenuFlyout.Closed -= OpenPropertiesFromItemContextMenuFlyout;
			
			FilePropertiesHelpers.OpenPropertiesWindow(ContentPageContext.ShellPage!);
		}

		private void OpenPropertiesFromBaseContextMenuFlyout(object? _, object e)
		{
			var page = ContentPageContext.ShellPage?.SlimContentPage;
			if (page is not null)
				page.BaseContextMenuFlyout.Closed -= OpenPropertiesFromBaseContextMenuFlyout;
			
			FilePropertiesHelpers.OpenPropertiesWindow(ContentPageContext.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
