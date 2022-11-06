using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleLayoutAdaptiveAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "Adaptive".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleLayoutAdaptive;
		public ShortKey ShortKey => new(VirtualKey.Number7, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public ToggleLayoutAdaptiveAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
			=> viewModel.PaneHolder?.ActivePane?.InstanceViewModel?.FolderSettings?.ToggleLayoutModeAdaptive();
	}
}
