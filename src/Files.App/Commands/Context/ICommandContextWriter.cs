using Files.App.ViewModels;

namespace Files.App.Commands
{
	public interface ICommandContextWriter
	{
		IShellPage? ShellPage { get; set; }
		ToolbarViewModel? ToolbarViewModel { get; set; }
	}
}
