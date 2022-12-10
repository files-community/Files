using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.ViewModels;

namespace Files.App.Commands
{
	public class CommandContext : ObservableObject, ICommandContext, ICommandContextWriter
	{
		private IShellPage? shellPage;
		public IShellPage? ShellPage
		{
			get => shellPage;
			set => SetProperty(ref shellPage, value);
		}

		private ToolbarViewModel? toolbarViewModel;
		public ToolbarViewModel? ToolbarViewModel
		{
			get => toolbarViewModel;
			set => SetProperty(ref toolbarViewModel, value);
		}
	}
}
