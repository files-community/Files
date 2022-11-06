using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.Backend.Services.Settings;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleShowFileExtensionsAction : IKeyboardAction
	{
		public string Label => "NavToolbarShowFileExtensionsHeader.Text".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleShowFileExtensions;
		public ShortKey ShortKey => ShortKey.None;

		public void Execute()
		{
			var settings = Ioc.Default.GetRequiredService<IPreferencesSettingsService>();
			settings.ShowFileExtensions = !settings.ShowFileExtensions;
		}
	}
}
