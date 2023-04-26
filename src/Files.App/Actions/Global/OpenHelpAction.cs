﻿using Files.App.Commands;
using Files.App.Extensions;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenHelpAction : IAction
	{
		public string Label { get; } = "Help".GetLocalizedResource();

		public string Description => "OpenHelpDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.F1);

		public async Task ExecuteAsync()
		{
			var url = new Uri(Constants.GitHub.DocumentationUrl);
			await Launcher.LaunchUriAsync(url);
		}
	}
}
