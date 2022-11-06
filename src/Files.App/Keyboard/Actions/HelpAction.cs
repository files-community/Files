using System;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class HelpAction : IKeyboardAction
	{
		public string Label => "Help";
		public string Description => "Opens the help in the web browser.";

		public KeyboardActionCodes Code => KeyboardActionCodes.Help;
		public ShortKey ShortKey => new(VirtualKey.F1);

		public async void Execute()
		{
			var url = new Uri(Constants.GitHub.DocumentationUrl);
			await Launcher.LaunchUriAsync(url);
		}
	}
}
