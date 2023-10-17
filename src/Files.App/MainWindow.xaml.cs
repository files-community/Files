// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.IO;
using Windows.ApplicationModel;
using WinUIEx;

namespace Files.App
{
	public sealed partial class MainWindow : WindowEx
	{
		private readonly IApplicationService ApplicationService;

		private static MainWindow? _Instance;
		public static MainWindow Instance => _Instance ??= new();

		public IntPtr WindowHandle { get; }

		private MainWindow()
		{
			ApplicationService = new ApplicationService();

			WindowHandle = this.GetWindowHandle();

			InitializeComponent();

			EnsureEarlyWindow();
		}

		private void EnsureEarlyWindow()
		{
			// Set PersistenceId
			PersistenceId = "FilesMainWindow";

			// Set minimum sizes
			MinHeight = 416;
			MinWidth = 516;

			AppWindow.Title = "Files";
			AppWindow.SetIcon(Path.Combine(Package.Current.InstalledLocation.Path, ApplicationService.AppIcoPath));
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			// Workaround for full screen window messing up the taskbar
			// https://github.com/microsoft/microsoft-ui-xaml/issues/8431
			InteropHelpers.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
		}
	}
}
