// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Win32.Foundation;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class ShellPreview : UserControl
	{
		private ShellPreviewViewModel ViewModel { get; set; }

		public ShellPreview(ShellPreviewViewModel model)
		{
			ViewModel = model;

			InitializeComponent();
		}

		private void PreviewHost_Loaded(object sender, RoutedEventArgs e)
		{
			ViewModel.LoadPreview(contentPresenter);
			ViewModel.SizeChanged(GetPreviewSize());

			if (XamlRoot.Content is FrameworkElement element)
			{
				element.SizeChanged += PreviewHost_SizeChanged;
				element.PointerEntered += PreviewHost_PointerEntered;
			}
		}

		private void PreviewHost_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ViewModel.SizeChanged(GetPreviewSize());
		}

		private RECT GetPreviewSize()
		{
			var source = contentPresenter.TransformToVisual(XamlRoot.Content);
			var physicalSize = contentPresenter.RenderSize;
			var physicalPos = source.TransformPoint(new Point(0, 0));
			var scale = XamlRoot.RasterizationScale;
			var result = RECT.FromXYWH(
				(int)(physicalPos.X * scale + 0.5),
				(int)(physicalPos.Y * scale + 0.5),
				(int)(physicalSize.Width * scale + 0.5),
				(int)(physicalSize.Height * scale + 0.5));

			return result;
		}

		private void PreviewHost_Unloaded(object sender, RoutedEventArgs e)
		{
			if (XamlRoot.Content is FrameworkElement element)
			{
				element.SizeChanged -= PreviewHost_SizeChanged;
				element.PointerEntered -= PreviewHost_PointerEntered;
			}

			ViewModel.UnloadPreview();
		}

		private void PreviewHost_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			ViewModel.PointerEntered(sender == contentPresenter);
		}
	}
}
