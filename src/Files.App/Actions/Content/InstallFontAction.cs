using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Shell;
using Files.Backend.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class InstallFontAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "Install".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public bool IsExecutable => context.SelectedItems.Any() &&
			context.SelectedItems.All(x => FileExtensionHelpers.IsFontFile(x.FileExtension)) &&
			context.PageType is not ContentPageTypes.RecycleBin;

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