using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleLayoutDetailsAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "Details".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleLayoutDetails;
		public ShortKey ShortKey => new(VirtualKey.Number1, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public ToggleLayoutDetailsAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
			=> viewModel.PaneHolder?.ActivePane?.InstanceViewModel?.FolderSettings?.ToggleLayoutModeDetailsView(true);
	}
}
