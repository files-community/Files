// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class OpenPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "OpenProperties".GetLocalizedResource();

		public string Description { get; } = "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconProperties");

		public HotKey HotKey { get; } = new(Keys.Enter, KeyModifiers.Menu);

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.Home &&
			!(context.PageType is ContentPageTypes.SearchResults && 
			!context.HasSelection);

		public OpenPropertiesAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var page = context.ShellPage?.SlimContentPage;

			if (page?.ItemContextMenuFlyout.IsOpen ?? false)
				page.ItemContextMenuFlyout.Closed += OpenPropertiesFromItemContextMenuFlyout;
			else if (page?.BaseContextMenuFlyout.IsOpen ?? false)
				page.BaseContextMenuFlyout.Closed += OpenPropertiesFromBaseContextMenuFlyout;
			else
				FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage!);

			return Task.CompletedTask;
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

		private void OpenPropertiesFromItemContextMenuFlyout(object? _, object e)
		{
			var page = context.ShellPage?.SlimContentPage;
			if (page is not null)
				page.ItemContextMenuFlyout.Closed -= OpenPropertiesFromItemContextMenuFlyout;
			
			FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage!);
		}

		private void OpenPropertiesFromBaseContextMenuFlyout(object? _, object e)
		{
			var page = context.ShellPage?.SlimContentPage;
			if (page is not null)
				page.BaseContextMenuFlyout.Closed -= OpenPropertiesFromBaseContextMenuFlyout;
			
			FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage!);
		}
	}
}
