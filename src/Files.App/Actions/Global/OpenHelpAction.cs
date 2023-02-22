using Files.App.Commands;
using Files.App.Extensions;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenHelpAction : IAction
	{
		public string Label { get; } = "Help".GetLocalizedResource();

		public HotKey HotKey { get; } = new(VirtualKey.F1);

		public async Task ExecuteAsync()
		{
			var url = new Uri(Core.Constants.GitHub.DocumentationUrl);
			await Launcher.LaunchUriAsync(url);
		}
	}
}
