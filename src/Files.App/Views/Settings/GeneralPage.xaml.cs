// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class GeneralPage : Page
	{
		private readonly GeneralViewModel ViewModel;

		public GeneralPage()
		{
			ViewModel = Ioc.Default.GetRequiredService<GeneralViewModel>();

			InitializeComponent();
		}
	}
}
