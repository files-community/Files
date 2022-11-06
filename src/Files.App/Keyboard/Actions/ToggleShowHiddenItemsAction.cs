using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.Backend.Services.Settings;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleShowHiddenItemsAction : IKeyboardAction
	{
		public string Label => "NavToolbarShowHiddenItemsHeader.Text".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleShowHiddenItems;
		public ShortKey ShortKey => ShortKey.None;

		public void Execute()
		{
			var settings = Ioc.Default.GetRequiredService<IUserSettingsService>();
			settings.FoldersSettingsService.ShowHiddenItems = !settings.FoldersSettingsService.ShowHiddenItems;
		}
	}
}
