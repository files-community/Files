// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class QuickAccessWidget : UserControl
	{
		public QuickAccessWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<QuickAccessWidgetViewModel>();

		public QuickAccessWidget()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Button_Click(sender ,e);
		}

		private void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			ViewModel.Button_PointerPressed(sender, e);
		}

		private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.Button_RightTapped(sender, e);
		}
	}
}
