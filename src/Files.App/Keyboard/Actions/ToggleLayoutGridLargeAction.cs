using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleLayoutGridLargeAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "LargeIcons".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleLayoutGridLarge;
		public ShortKey ShortKey => new(VirtualKey.Number5, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public ToggleLayoutGridLargeAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
			=> viewModel.PaneHolder?.ActivePane?.InstanceViewModel?.FolderSettings?.ToggleLayoutModeGridViewLarge(true);
	}
}
