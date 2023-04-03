using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Shell;
using Files.Backend.Helpers;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class RunWithPowershellAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public bool IsExecutable => context.SelectedItem is not null &&
			FileExtensionHelpers.IsPowerShellFile(context.SelectedItem.FileExtension);

		public string Label => "RunWithPowerShell".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph => new("\uE756");

		public RunWithPowershellAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			Win32API.RunPowershellCommand($"{context.ShellPage?.SlimContentPage?.SelectedItem.ItemPath}", false);
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
