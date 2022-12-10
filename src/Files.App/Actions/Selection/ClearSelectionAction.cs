using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class ClearSelectionAction : ObservableObject, IAction
	{
		private readonly ICommandContext context = Ioc.Default.GetRequiredService<ICommandContext>();

		public CommandCodes Code => CommandCodes.ClearSelection;
		public string Label => "NavToolbarClearSelection/Text".GetLocalizedResource();

		public IGlyph Glyph { get; } = new Glyph("\uE8E6");

		public bool IsExecutable => context.ToolbarViewModel?.SelectedItems?.Any() ?? false;

		public ClearSelectionAction()
		{
			context.PropertyChanging += Context_PropertyChanging;
			context.PropertyChanged += Context_PropertyChanged;
		}

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
				pane?.SlimContentPage?.ItemManipulationModel?.ClearSelection();
		}

		private void Context_PropertyChanging(object? _, PropertyChangingEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel))
			{
				if (context.ToolbarViewModel is not null)
					context.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
		private void Context_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel))
			{
				if (context.ToolbarViewModel is not null)
					context.ToolbarViewModel.PropertyChanged += ToolbarViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}

		private void ToolbarViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ToolbarViewModel.SelectedItems))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
