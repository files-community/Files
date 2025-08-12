// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Microsoft.Win32;

namespace Files.App.Actions
{
	internal sealed partial class OpenLogFileLocationAction : IAction
	{
		public string Label
			=> Strings.OpenLogLocation.GetLocalizedResource();

		public string Description
			=> Strings.OpenLogFileLocationDescription.GetLocalizedResource();
		
		public HotKey HotKey
			=> new(Keys.OemPeriod, KeyModifiers.CtrlShift);

		public async Task ExecuteAsync(object? parameter = null)
		{
			await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask();
		}
	}
}