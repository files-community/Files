// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;

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
			await dialogService.ShowDialogAsync(viewModel);
		}
	}
}
