using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class SetAsSlideshowBackgroundAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "SetAsSlideshow".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new("\uE91B");

		public bool IsExecutable => context.ShellPage is not null &&
			context.SelectedItems.Count > 1 &&
			context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder &&
			(context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public SetAsSlideshowBackgroundAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var paths = context.SelectedItems.Select(item => item.ItemPath).ToArray();
			WallpaperHelpers.SetSlideshow(paths);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IContentPageContext.SelectedItems):
					if (context.ShellPage is not null && context.ShellPage.SlimContentPage is not null)
					{
						var viewModel = context.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
						var extensions = context.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

						viewModel.CheckAllFileExtensions(extensions);
					}

					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
