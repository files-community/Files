// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace Files.App.UnitTests
{
	public partial class App : Application
	{
		private Window? _window;

		public App()
		{
			InitializeComponent();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();

			_window = new MainWindow();
			_window.Activate();

			UITestMethodAttribute.DispatcherQueue = _window.DispatcherQueue;

			Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(System.Environment.CommandLine);
		}
	}
}
