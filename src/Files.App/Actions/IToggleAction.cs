namespace Files.App.Actions
{
	public interface IToggleAction : IAction
	{
		bool IsOn { get; }
	}
}
