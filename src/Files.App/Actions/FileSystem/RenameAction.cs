using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class RenameAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Rename".GetLocalizedResource();

		public string Description { get; } = "RenameDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.F2);

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconRename");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			IsPageTypeValid() &&
			context.ShellPage.SlimContentPage is not null &&
			IsSelectionValid();

		public RenameAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel.StartRenameItem();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private bool IsSelectionValid()
		{
			return context.HasSelection && context.SelectedItems.Count == 1;
		}

		private bool IsPageTypeValid()
		{
			return context.PageType is
				not ContentPageTypes.None and
				not ContentPageTypes.Home and
				not ContentPageTypes.RecycleBin and
				not ContentPageTypes.ZipFolder;
		}
	}
}
