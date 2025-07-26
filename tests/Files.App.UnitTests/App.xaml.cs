// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Files.App.UnitTests
{
	[TestClass]
	public unsafe partial class App : Application
	{
		private Window? _window;

		public App()
		{
			InitializeComponent();
		}

		[AssemblyInitialize]
		public static void InitializeAssembly(TestContext context)
		{
			PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED);
		}

		[AssemblyCleanup]
		public static void CleanupAssembly()
		{
			PInvoke.CoUninitialize();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			UnitTestClient.CreateDefaultUI();

			_window = new MainWindow();
			_window.Activate();

			UITestMethodAttribute.DispatcherQueue = _window.DispatcherQueue;

			UnitTestClient.Run(Environment.CommandLine);
		}
	}
}
