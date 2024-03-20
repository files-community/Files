// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Actions
{
	internal sealed class OpenHelpAction : IAction
	{
		public string Label
			=> "Help".GetLocalizedResource();

		public string Description
			=> "OpenHelpDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F1);

		public Task ExecuteAsync()
		{
			var url = new Uri(Constants.ExternalUrl.DocumentationUrl);
			return Launcher.LaunchUriAsync(url).AsTask();
		}
	}
}
