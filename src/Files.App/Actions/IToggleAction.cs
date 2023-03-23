namespace Files.App.Actions
{
	public interface IToggleAction : IAction
	{
		/// <summary>
		/// Returns whether the toggle is on or not.
		/// </summary>
		bool IsOn { get; }
	}
}
