// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.UITests
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			MainWindow.Instance.Activate();
		}
	}
}
