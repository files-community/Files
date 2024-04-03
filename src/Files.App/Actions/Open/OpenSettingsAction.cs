// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenSettingsAction : BaseUIAction, IAction
	{
		public string Label
			=> "Settings".GetLocalizedResource();

		public string Description
			=> "OpenSettingsDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.Ctrl);

		public Task ExecuteAsync()
		{
			NavigationHelpers.OpenSettings();

			return Task.CompletedTask;
		}
	}
}
