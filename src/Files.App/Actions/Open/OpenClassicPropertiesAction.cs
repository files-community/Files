// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Vanara.PInvoke;

namespace Files.App.Actions
{
	internal sealed class OpenClassicPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenClassicProperties".GetLocalizedResource();

		public string Description
			=> "OpenClassicPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Properties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.AltShift);

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.Home &&
			!(context.PageType is ContentPageTypes.SearchResults &&
			!context.HasSelection);

		public OpenClassicPropertiesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var itemPaths = context.HasSelection && context.SelectedItems is not null
				? context.SelectedItems.Select(x => x.ItemPath).ToArray()
				: new[] { context.Folder!.ItemPath };

			if (itemPaths.Length > 0)
				await ContextMenu.InvokeVerb("properties", itemPaths);
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
