using Files.App.ViewModels;

namespace Files.App.Keyboard.Actions
{
	internal class ClearSelectionAction : IKeyboardAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label => "ClearSelection";
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ClearSelection;
		public ShortKey ShortKey => ShortKey.None;

		public ClearSelectionAction(SidebarViewModel viewModel) => this.viewModel = viewModel;

		public void Execute()
		{
			var pane = viewModel.PaneHolder?.ActivePaneOrColumn;

			bool isEditing = pane?.ToolbarViewModel?.IsEditModeEnabled ?? true;
			bool isRenaming = pane?.SlimContentPage?.IsRenamingItem ?? true;

			if (!isEditing && !isRenaming)
				pane?.SlimContentPage?.ItemManipulationModel?.ClearSelection();
		}
	}
}
