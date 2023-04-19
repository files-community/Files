using Files.App.Commands;
using Files.App.Contexts;
using Files.App.DataModels;

namespace Files.App.Actions
{
	internal class PasteItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Paste".GetLocalizedResource();

		public string Description => "PasteItemDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconPaste");

		public HotKey HotKey { get; } = new(Keys.V, KeyModifiers.Ctrl);

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public PasteItemAction()
		{
			isExecutable = GetIsExecutable();

			context.PropertyChanged += Context_PropertyChanged;
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is null)
				return;

			string path = context.ShellPage.FilesystemViewModel.WorkingDirectory;
			await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
		}

		public bool GetIsExecutable()
		{
			return App.AppModel.IsPasteEnabled
				&& context.PageType is not ContentPageTypes.Home and not ContentPageTypes.RecycleBin and not ContentPageTypes.SearchResults;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
		}
		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
				SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
		}
	}
}
