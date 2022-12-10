using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
    internal class RenameAction : ObservableObject, IAction
	{
		private readonly ICommandContext? context = Ioc.Default.GetService<ICommandContext>();

		public CommandCodes Code => CommandCodes.Rename;
		public string Label => "BaseLayoutItemContextFlyoutRename/Text".GetLocalizedResource();

		public IGlyph Glyph { get; } = new Glyph("\uF027", "\uF028");
		public HotKey HotKey { get; } = new(VirtualKey.F2);

		public bool IsExecutable
			=> (context?.ToolbarViewModel?.CanRename ?? false)
			&& (context?.ShellPage?.InstanceViewModel?.IsPageTypeNotHome ?? false);

		public RenameAction()
		{
			if (context is not null)
			{
				context.PropertyChanging += Context_PropertyChanging;
				context.PropertyChanged += Context_PropertyChanged;
			}
		}

		public Task ExecuteAsync()
		{
			Execute();
			return Task.CompletedTask;
		}

		private void Execute() => context?.ShellPage?.SlimContentPage?.ItemManipulationModel?.StartRenameItem();

		private void Context_PropertyChanging(object? _, PropertyChangingEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel) && context?.ToolbarViewModel is not null)
			{
				context.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
		private void Context_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel) && context?.ToolbarViewModel is not null)
			{
				context.ToolbarViewModel.PropertyChanged += ToolbarViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}

		private void ToolbarViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ToolbarViewModel.CanRename))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
