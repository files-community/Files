// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class QuickAccessWidget : UserControl
	{
		private QuickAccessWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<QuickAccessWidgetViewModel>();

		public QuickAccessWidget()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{

        }

		private void Button_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{

        }

		private void Button_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
		{

        }
    }
}
