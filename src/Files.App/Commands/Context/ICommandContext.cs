using Files.App.ViewModels;
using System.ComponentModel;

namespace Files.App.Commands
{
	public interface ICommandContext : INotifyPropertyChanged, INotifyPropertyChanging
	{
		IShellPage? ShellPage { get; }
		ToolbarViewModel? ToolbarViewModel { get; }
	}
}
