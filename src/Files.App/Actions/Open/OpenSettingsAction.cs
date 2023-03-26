using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.Backend.Extensions;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class OpenSettingsAction : IAction
	{
		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly SettingsDialogViewModel viewModel = new();

		public string Label => "Settings".GetLocalizedResource();

		public string Description => "Settings".GetLocalizedResource();

		public async Task ExecuteAsync()
		{
			var dialog = dialogService.GetDialog(viewModel);
			await dialog.TryShowAsync();
		}
	}
}
