using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Views;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class OpenParentFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "BaseLayoutItemContextFlyoutOpenParentFolder/Text".GetLocalizedResource();

		public RichGlyph Glyph => new(baseGlyph: "\uE197");

		public bool IsExecutable => context.HasSelection && context.ShellPage.InstanceViewModel.IsPageTypeSearchResults;

		public OpenParentFolderAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var item = context.SelectedItem;
			var folderPath = Path.GetDirectoryName(item.ItemPath.TrimEnd('\\'));

			context.ShellPage.NavigateWithArguments(context.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
			{
				NavPathParam = folderPath,
				SelectItems = new[] { item.ItemNameRaw },
				AssociatedTabInstance = context.ShellPage
			});
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
