// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;
using Windows.System;

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