// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class LayoutPage : Page
	{
		public LayoutPage()
		{
			InitializeComponent();
			Loaded += (_, _) => ViewModel.StartListening();
			Unloaded += (_, _) => ViewModel.StopListening();
		}
	}
}
