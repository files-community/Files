using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Keyboard.Actions
{
	internal class SelectAllAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "SelectAll";
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.SelectAll;
		public ShortKey ShortKey => new(VirtualKey.A, VirtualKeyModifiers.Control);

		public SelectAllAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
		{
			var pane = viewModel.PaneHolder?.ActivePaneOrColumn;

			bool isEditing = pane?.ToolbarViewModel?.IsEditModeEnabled ?? true;
			bool isRenaming = pane?.SlimContentPage?.IsRenamingItem ?? true;

			if (!isEditing && !isRenaming)
				pane?.SlimContentPage?.ItemManipulationModel?.SelectAllItems();
		}
	}
}
