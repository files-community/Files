using Files.App.Commands;
using Files.App.Contexts;
using Files.Backend.Helpers;

namespace Files.App.Actions
{
	internal class PlayAllAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "PlayAll".GetLocalizedResource();

		public string Description { get; } = "PlayAllDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE768");

		public bool IsExecutable => context.PageType is not ContentPageTypes.RecycleBin &&
			context.SelectedItems.Count > 1 &&
			context.SelectedItems.All(item => FileExtensionHelpers.IsMediaFile(item.FileExtension));

		public PlayAllAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return NavigationHelpers.OpenSelectedItems(context.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
