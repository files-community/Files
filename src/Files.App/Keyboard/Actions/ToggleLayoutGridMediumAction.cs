using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleLayoutGridMediumAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "MediumIcons".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleLayoutGridMedium;
		public ShortKey ShortKey => new(VirtualKey.Number4, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public ToggleLayoutGridMediumAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
			=> viewModel.PaneHolder?.ActivePane?.InstanceViewModel?.FolderSettings?.ToggleLayoutModeGridViewMedium(true);
	}
}
