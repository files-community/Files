using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class LaunchQuickLookAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public HotKey HotKey { get; } = new(VirtualKey.Space);

		public bool CanExecute => context.SelectedItems.Count == 1 &&
			(!context.ShellPage?.ToolbarViewModel?.IsEditModeEnabled ?? false) &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public string Label => "LaunchQuickLook".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

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
					NotifyCanExecuteChanged();
					var _ = SwitchQuickLookPreview();
					break;
			}
		}

		private async Task SwitchQuickLookPreview()
		{
			if (CanExecute)
				await QuickLookHelpers.ToggleQuickLook(context.SelectedItem!.ItemPath, true);
		}
	}
}
