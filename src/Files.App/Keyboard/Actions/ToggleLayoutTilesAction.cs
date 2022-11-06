using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleLayoutTilesAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "Tiles".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleLayoutTiles;
		public ShortKey ShortKey => new(VirtualKey.Number2, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public ToggleLayoutTilesAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
			=> viewModel.PaneHolder?.ActivePane?.InstanceViewModel?.FolderSettings?.ToggleLayoutModeTiles(true);
	}
}
