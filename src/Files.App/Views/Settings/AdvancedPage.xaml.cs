// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class AdvancedPage : Page
	{
		private readonly AdvancedViewModel ViewModel;

		public AdvancedPage()
		{
			ViewModel = Ioc.Default.GetRequiredService<AdvancedViewModel>();

			InitializeComponent();
		}
	}
}
