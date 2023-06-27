// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Actions
{
	internal class OpenHelpAction : IAction
	{
		public string Label
			=> "Help".GetLocalizedResource();

		public string Description
			=> "OpenHelpDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F1);

		public Task ExecuteAsync()
		{
			var url = new Uri(Constants.GitHub.DocumentationUrl);
			return Launcher.LaunchUriAsync(url).AsTask();
		}
	}
}
