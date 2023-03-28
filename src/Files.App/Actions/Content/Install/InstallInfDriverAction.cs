using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Shell;
using Files.Backend.Helpers;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class InstallInfDriverAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "Install".GetLocalizedResource();
		
		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new("\uE9F5");

		public bool CanExecute => context.SelectedItems.Count == 1 &&
			FileExtensionHelpers.IsInfFile(context.SelectedItems[0].FileExtension) &&
			context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder;

		public InstallInfDriverAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (ListedItem selectedItem in context.SelectedItems)
				await Win32API.InstallInf(selectedItem.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.PageType):
					NotifyCanExecuteChanged();
					break;
			}
		}
	}
}
