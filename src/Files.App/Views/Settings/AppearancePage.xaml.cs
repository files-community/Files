// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class AppearancePage : Page
	{
		private readonly AppearanceViewModel ViewModel;

		public AppearancePage()
		{
			ViewModel = Ioc.Default.GetRequiredService<AppearanceViewModel>();

			InitializeComponent();
		}
	}
}
