using Files.App.Commands;
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
