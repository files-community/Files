using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Files.App.Actions.Content.ImageEdition
{
	internal class RotateRightAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "RotateRight".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRotateRight");

		public bool IsExecutable => context.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel.IsSelectedItemImage;

		public RotateRightAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (var image in context.SelectedItems)
				await BitmapHelper.Rotate(PathNormalization.NormalizePath(image.ItemPath), BitmapRotation.Clockwise90Degrees);

			context.ShellPage.SlimContentPage.ItemManipulationModel.RefreshItemsThumbnail();
			App.PreviewPaneViewModel.UpdateSelectedItemPreview();
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
