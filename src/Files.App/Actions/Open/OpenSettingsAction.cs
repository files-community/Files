using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class OpenSettingsAction : BaseUIAction, IAction
	{
		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly SettingsDialogViewModel viewModel = new();

		public string Label => "Settings".GetLocalizedResource();

		public string Description => "OpenSettingsDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.OemComma, KeyModifiers.Ctrl);

		public async Task ExecuteAsync()
		{
			var dialog = dialogService.GetDialog(viewModel);
			await dialog.TryShowAsync();
		}
	}
}
