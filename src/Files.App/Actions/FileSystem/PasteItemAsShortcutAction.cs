// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class PasteItemAsShortcutAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.PasteShortcut.GetLocalizedResource();

		public string Description
			=> Strings.PasteShortcutDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Paste");

		public bool IsExecutable
			=> GetIsExecutable();

		public PasteItemAsShortcutAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is not { } shellPage)
				return Task.CompletedTask;

			var shellViewModel = shellPage.GetRequiredShellViewModel();
			string path = shellViewModel.WorkingDirectory!;
			return UIFilesystemHelpers.PasteItemAsShortcutAsync(path, shellPage);
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
