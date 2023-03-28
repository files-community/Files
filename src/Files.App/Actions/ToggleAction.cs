using Microsoft.UI.Xaml.Input;

namespace Files.App.Actions
{
	public abstract class ToggleAction : XamlUICommand
	{
		/// <summary>
		/// Returns whether the toggle is on or not.
		/// </summary>
		public bool IsOn() => true;

		public new void NotifyCanExecuteChanged()
		{
			base.NotifyCanExecuteChanged();
			IsOn();
		}
	}
}
