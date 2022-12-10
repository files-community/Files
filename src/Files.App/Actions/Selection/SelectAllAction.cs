using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class SelectAllAction : ObservableObject, IAction
	{
		private readonly ICommandContext context = Ioc.Default.GetRequiredService<ICommandContext>();

		public CommandCodes Code => CommandCodes.SelectAll;
		public string Label => "NavToolbarSelectAll/Text".GetLocalizedResource();

		public IGlyph Glyph { get; } = new Glyph("\uE8B3");
		public HotKey HotKey { get; } = new(VirtualKey.A, VirtualKeyModifiers.Control);

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
				pane?.SlimContentPage?.ItemManipulationModel?.SelectAllItems();
		}
	}
}
