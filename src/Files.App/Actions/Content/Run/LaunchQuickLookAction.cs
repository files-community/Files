using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Shell;
using Files.Backend.Helpers;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class LaunchQuickLookAction : ObservableObject, IAction
	{
		public HotKey HotKey { get; } = new(VirtualKey.Space);

		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		public bool IsExecutable => context.SelectedItem is not null &&
			(!context.ShellPage?.ToolbarViewModel?.IsEditModeEnabled ?? false) &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public string Label => "LaunchQuickLook".GetLocalizedResource();

		public LaunchQuickLookAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await QuickLookHelpers.ToggleQuickLook(context.SelectedItem!.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
