// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class PasteItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.Paste.GetLocalizedResource();

		public string Description
			=> Strings.PasteItemDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Paste");

		public HotKey HotKey
			=> new(Keys.V, KeyModifiers.Ctrl);

		public bool IsExecutable
			=> GetIsExecutable();

		public PasteItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			string path = context.ShellPage.ShellViewModel.WorkingDirectory;
			await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
		}

		public bool GetIsExecutable()
		{
			return
				App.AppModel.IsPasteEnabled &&
				context.PageType != ContentPageTypes.Home &&
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.SearchResults &&
				context.PageType != ContentPageTypes.ReleaseNotes &&
				context.PageType != ContentPageTypes.Settings;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
