using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class InvertSelectionAction : IAction
	{
		private readonly ICommandContext context = Ioc.Default.GetRequiredService<ICommandContext>();

		public CommandCodes Code => CommandCodes.InvertSelection;
		public string Label => "NavToolbarInvertSelection/Text".GetLocalizedResource();

		public IGlyph Glyph { get; } = new Glyph("\uE746");

		public Task ExecuteAsync()
		{
			Execute();
			return Task.CompletedTask;
		}

		public void Execute()
		{
			var pane = context.ShellPage;

			bool isEditing = pane?.ToolbarViewModel?.IsEditModeEnabled ?? true;
			bool isRenaming = pane?.SlimContentPage?.IsRenamingItem ?? true;

			if (!isEditing && !isRenaming)
				pane?.SlimContentPage?.ItemManipulationModel?.InvertSelection();
		}
	}
}
