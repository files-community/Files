// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenClassicPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenClassicProperties".GetLocalizedResource();

		public string Description
			=> "OpenClasicPropertiesDescription".GetLocalizedResource();

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

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems is not null)
			{
				foreach (var item in context.SelectedItems)
				{
					Process.Start("explorer.exe", $"/select,\"{item.ItemPath}\"");
				}
			}

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
	}
}
