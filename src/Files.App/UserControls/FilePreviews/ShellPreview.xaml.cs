using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Vanara.PInvoke.Shell32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class ShellPreview : UserControl
	{
		private ShellPreviewViewModel ViewModel { get; set; }

		public ShellPreview(ShellPreviewViewModel model)
		{
			ViewModel = model;
			this.InitializeComponent();
		}

		private void PreviewHost_GotFocus(object sender, RoutedEventArgs e)
		{
			ViewModel.GotFocus(() => contentPresenter.Focus(FocusState.Programmatic));
		}

		private void PreviewHost_Loaded(object sender, RoutedEventArgs e)
		{
			ViewModel.LoadPreview();
		}

		private void PreviewHost_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var source = contentPresenter.TransformToVisual(XamlRoot.Content);
			var physicalSize = contentPresenter.RenderSize;
			var physicalPos = source.TransformPoint(new Point(0, 0));
			var result = new Vanara.PInvoke.RECT();
			result.Left = (int)(physicalPos.X + 0.5);
			result.Top = (int)(physicalPos.Y + 0.5);
			result.Right = (int)(physicalPos.X + physicalSize.Width + 0.5);
			result.Bottom = (int)(physicalPos.Y + physicalSize.Height + 0.5);
			ViewModel.SizeChanged(result);
		}
	}
}
