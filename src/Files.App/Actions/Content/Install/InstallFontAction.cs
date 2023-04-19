using Files.App.Contexts;
using Files.App.Shell;
using Files.Backend.Helpers;

namespace Files.App.Actions
{
	internal class InstallFontAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "Install".GetLocalizedResource();

		public string Description => "InstallFontDescription".GetLocalizedResource();

		public bool IsExecutable => context.SelectedItems.Any() &&
			context.SelectedItems.All(x => FileExtensionHelpers.IsFontFile(x.FileExtension)) &&
			context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder;

		public InstallFontAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			foreach (ListedItem selectedItem in context.SelectedItems)
				Win32API.InstallFont(selectedItem.ItemPath, false);

			return Task.CompletedTask;
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}