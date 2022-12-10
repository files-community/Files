using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
    internal class PropertiesAction : ObservableObject, IAction
	{
		private readonly ICommandContext? context = Ioc.Default.GetService<ICommandContext>();

		public CommandCodes Code => CommandCodes.Properties;
		public string Label => "BaseLayoutItemContextFlyoutProperties/Text".GetLocalizedResource();

		public IGlyph Glyph { get; } = new Glyph("\uF031", "\uF032");

		public bool IsExecutable
			=> (context?.ToolbarViewModel?.CanViewProperties ?? false)
			&& (context?.ShellPage?.InstanceViewModel?.IsPageTypeNotHome ?? false);

		public PropertiesAction()
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

		private void Execute()
		{
			var flyout = context?.ShellPage?.SlimContentPage?.ItemContextMenuFlyout;
			if (flyout is not null)
			{
				if (flyout.IsOpen)
					flyout.Closed += OpenProperties;
				else
					FilePropertiesHelpers.ShowProperties(context?.ShellPage!);
			}
		}

		private void OpenProperties(object? _, object e)
		{
			var flyout = context?.ShellPage?.SlimContentPage?.ItemContextMenuFlyout;
			if (flyout is not null)
			{
				flyout.Closed -= OpenProperties;
				FilePropertiesHelpers.ShowProperties(context?.ShellPage!);
			}
		}

		private void Context_PropertyChanging(object? _, PropertyChangingEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ShellPage) && context?.ShellPage?.InstanceViewModel is not null)
			{
				context.ShellPage.InstanceViewModel.PropertyChanged -= InstanceViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel) && context?.ToolbarViewModel is not null)
			{
				context.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
		private void Context_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ICommandContext.ShellPage) && context?.ShellPage?.InstanceViewModel is not null)
			{
				context.ShellPage.InstanceViewModel.PropertyChanged += InstanceViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
			if (e.PropertyName is nameof(ICommandContext.ToolbarViewModel) && context?.ToolbarViewModel is not null)
			{
				context.ToolbarViewModel.PropertyChanged += ToolbarViewModel_PropertyChanged;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}

		private void InstanceViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(CurrentInstanceViewModel.IsPageTypeNotHome))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void ToolbarViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ToolbarViewModel.CanViewProperties))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
