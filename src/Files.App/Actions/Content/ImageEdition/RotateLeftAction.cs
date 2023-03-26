using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal class RotateLeftAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "RotateLeft".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRotateLeft");

		public bool IsExecutable => IsContextPageTypeAdaptedToCommand()
						&& (context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public RotateLeftAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (var image in context.SelectedItems)
				await BitmapHelper.Rotate(PathNormalization.NormalizePath(image.ItemPath), BitmapRotation.Clockwise270Degrees);

			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.RefreshItemsThumbnail();
			Ioc.Default.GetRequiredService<PreviewPaneViewModel>().UpdateSelectedItemPreview();
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
			{
				if (context.ShellPage is not null && context.ShellPage.SlimContentPage is not null)
				{
					var viewModel = context.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
					var extensions = context.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

					viewModel.CheckAllFileExtensions(extensions);
				}

				OnPropertyChanged(nameof(IsExecutable));
			}
		}
	}
}
