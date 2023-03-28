using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Extensions;
using Files.Backend.Extensions;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenSettingsAction : XamlUICommand
	{
		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly SettingsDialogViewModel viewModel = new();

		public string Label => "Settings".GetLocalizedResource();

		public string Description => "Settings".GetLocalizedResource();

		public HotKey HotKey { get; } = new((VirtualKey)188, VirtualKeyModifiers.Control);

		public async Task ExecuteAsync()
		{
			var dialog = dialogService.GetDialog(viewModel);
			await dialog.TryShowAsync();
		}
	}
}
