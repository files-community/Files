using Files.App.Commands;
using Files.App.DataModels;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
    internal class HelpAction : IAction
	{
		public CommandCodes Code => CommandCodes.Help;

		public string Label => "Help";

		public HotKey HotKey => new(VirtualKey.F1);

		public async Task ExecuteAsync()
		{
			var url = new Uri(Constants.GitHub.DocumentationUrl);
			await Launcher.LaunchUriAsync(url);
		}
	}
}
