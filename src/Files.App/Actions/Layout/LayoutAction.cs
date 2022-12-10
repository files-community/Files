using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.DataModels;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
    internal abstract class LayoutAction : ObservableObject, IAction
	{
		private readonly ICommandContext? context = Ioc.Default.GetService<ICommandContext>();

		protected ToolbarViewModel? ToolbarViewModel
			=> context?.ToolbarViewModel;
		protected FolderSettingsViewModel? FolderSettingsViewModel
			=> context?.ShellPage?.InstanceViewModel?.FolderSettings;

		public abstract CommandCodes Code { get; }
		public abstract string Label { get; }

		public virtual IGlyph Glyph => Commands.Glyph.None;
		public virtual HotKey HotKey => HotKey.None;

		public virtual bool IsOn => false;
		public virtual bool IsExecutable => true;

		protected abstract string IsOnProperty { get; }

		public LayoutAction()
		{
			if (context is null)
				return;

			context.PropertyChanging += Context_PropertyChanging;
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			Execute();
			return Task.CompletedTask;
		}

		protected abstract void Execute();

		private void Context_PropertyChanging(object? _, PropertyChangingEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel) && context?.ToolbarViewModel is not null)
			{
				context.ToolbarViewModel.PropertyChanging -= ToolbarViewModel_PropertyChanging;
				context.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;

				OnPropertyChanging(nameof(IsOn));
				OnPropertyChanging(nameof(IsExecutable));
			}
		}
		private void Context_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel) && context?.ToolbarViewModel is not null)
			{
				context.ToolbarViewModel.PropertyChanging += ToolbarViewModel_PropertyChanging;
				context.ToolbarViewModel.PropertyChanged += ToolbarViewModel_PropertyChanged;

				OnPropertyChanged(nameof(IsOn));
				OnPropertyChanged(nameof(IsExecutable));
			}
		}

		private void ToolbarViewModel_PropertyChanging(object? _, PropertyChangingEventArgs e)
		{
			if (e.PropertyName is nameof(ToolbarViewModel.IsAdaptiveLayoutEnabled) || e.PropertyName == IsOnProperty)
			{
				OnPropertyChanging(nameof(IsOn));
				OnPropertyChanging(nameof(IsExecutable));
			}
		}
		private void ToolbarViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ToolbarViewModel.IsAdaptiveLayoutEnabled) || e.PropertyName == IsOnProperty)
			{
				OnPropertyChanged(nameof(IsOn));
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
	}
}
