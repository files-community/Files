// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenSettingsAction : BaseUIAction, IAction
	{
		private IDialogService DialogService { get; } = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly SettingsDialogViewModel viewModel = new();

		public string Label
			=> "Settings".GetLocalizedResource();

		public string Description
			=> "OpenSettingsDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.Ctrl);

		public Task ExecuteAsync()
		{
			var dialog = DialogService.GetDialog(viewModel);
			return dialog.TryShowAsync();
		}
	}
}
