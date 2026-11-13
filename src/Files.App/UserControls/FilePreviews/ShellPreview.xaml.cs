// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Win32.Foundation;
using WinRT;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class ShellPreview : UserControl
	{
		private ShellPreviewViewModel ViewModel { get; set; }
		private FrameworkElement? rootElement;
		private CancellationTokenSource? previewCancellation;

		public ShellPreview(ShellPreviewViewModel model)
		{
			ViewModel = model;

			InitializeComponent();
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private async void PreviewHost_Loaded(object sender, RoutedEventArgs e)
		{
			UnloadPreview();
			previewCancellation = new CancellationTokenSource();
			var loadToken = previewCancellation.Token;

			await SafetyExtensions.IgnoreExceptions(() => DispatcherQueue.EnqueueDeferredAsync(async () =>
			{
				await ViewModel.LoadPreviewAsync(contentPresenter, loadToken);
				if (loadToken.IsCancellationRequested)
					return;

				rootElement = XamlRoot.Content as FrameworkElement;
				if (rootElement is not null)
				{
					rootElement.SizeChanged += PreviewHost_SizeChanged;
					rootElement.PointerEntered += PreviewHost_PointerEntered;
					await ViewModel.SizeChangedAsync(GetPreviewSize(), loadToken);
				}
			}), App.Logger);
		}

		private async void PreviewHost_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (rootElement is not null && previewCancellation is not null)
			{
				var loadToken = previewCancellation.Token;
				var size = GetPreviewSize();
				await SafetyExtensions.IgnoreExceptions(
					() => DispatcherQueue.EnqueueDeferredAsync(() => ViewModel.SizeChangedAsync(size, loadToken)), App.Logger);
			}
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
			=> UnloadPreview();

		private void PreviewHost_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			ViewModel.PointerEntered(ReferenceEquals(sender, contentPresenter));
		}

		// Cancels pending loading and releases the native preview on its UI thread.
		public void UnloadPreview()
		{
			var cancellation = previewCancellation;
			previewCancellation = null;
			var loadToken = cancellation?.Token;
			cancellation?.Cancel();
			cancellation?.Dispose();

			if (rootElement is not null)
			{
				rootElement.SizeChanged -= PreviewHost_SizeChanged;
				rootElement.PointerEntered -= PreviewHost_PointerEntered;
				rootElement = null;
			}

			if (loadToken is { } token)
				_ = SafetyExtensions.IgnoreExceptions(() => ViewModel.UnloadPreviewAsync(token), App.Logger);
		}
	}
}
