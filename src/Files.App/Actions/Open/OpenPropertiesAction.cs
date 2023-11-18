// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenPropertiesAction : ObservableObject, IExtendedAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenProperties".GetLocalizedResource();

		public string Description
			=> "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconProperties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.Menu);

		public bool IsExecutable =>
			(context.PageType is not ContentPageTypes.Home &&
			!(context.PageType is ContentPageTypes.SearchResults && 
			!context.HasSelection)) ||
			Parameter is not null;

		public object? Parameter { get; set; }

		public OpenPropertiesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (Parameter is not null && Parameter is DriveCardItem item)
			{
				FilePropertiesHelpers.OpenPropertiesWindow(item.Item, context.ShellPage!);
			}
			else
			{
				FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage!);
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
