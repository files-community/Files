// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class AppearancePage : Page
	{
		private AppearanceViewModel ViewModel
			=> DataContext as AppearanceViewModel
				?? throw new InvalidOperationException("The appearance view model has not been initialized.");

		public AppearancePage()
		{
			DataContext = Ioc.Default.GetRequiredService<AppearanceViewModel>();

			InitializeComponent();
		}
	}
}
