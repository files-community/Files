using Microsoft.UI.Xaml.Input;
using Files.App.Commands;
using Files.App.Extensions;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenHelpAction : XamlUICommand
	{
		public string Label { get; } = "Help".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.F1);

		public async Task ExecuteAsync()
		{
			var url = new Uri(Constants.GitHub.DocumentationUrl);
			await Launcher.LaunchUriAsync(url);
		}
	}
}
