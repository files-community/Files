using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleLayoutGridSmallAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "SmallIcons".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleLayoutGridSmall;
		public ShortKey ShortKey => new(VirtualKey.Number3, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public ToggleLayoutGridSmallAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
			=> viewModel.PaneHolder?.ActivePane?.InstanceViewModel?.FolderSettings?.ToggleLayoutModeGridViewSmall(true);
	}
}
