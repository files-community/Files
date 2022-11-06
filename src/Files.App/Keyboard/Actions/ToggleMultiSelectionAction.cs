using Files.App.DataModels;

namespace Files.App.Keyboard.Actions
{
	internal class ToggleMultiSelectionAction : IKeyboardAction
	{
		private readonly AppModel model;

		public string Label => "MultiSelection";
		public string Description => string.Empty;

		public KeyboardActionCodes Code => KeyboardActionCodes.ToggleMultiSelection;
		public ShortKey ShortKey => ShortKey.None;

		public ToggleMultiSelectionAction(AppModel model) => this.model = model;

		public void Execute() => model.MultiselectEnabled = !model.MultiselectEnabled;
	}
}
