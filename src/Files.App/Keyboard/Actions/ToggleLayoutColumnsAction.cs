using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleLayoutColumnsAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "Columns".GetLocalizedResource();
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleLayoutColumns;
		public ShortKey ShortKey => new(VirtualKey.Number6, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public ToggleLayoutColumnsAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
			=> viewModel.PaneHolder?.ActivePane?.InstanceViewModel?.FolderSettings?.ToggleLayoutModeColumnView(true);
	}
}
