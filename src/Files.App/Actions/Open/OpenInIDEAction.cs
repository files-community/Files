// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenInIDEAction : ObservableObject, IAction
	{
		private readonly IDevToolsSettingsService _devToolsSettingsService;

		private readonly IContentPageContext _context;

		public string Label
			=> string.Format(
				"OpenInIDE".GetLocalizedResource(),
				_devToolsSettingsService.FriendlyIDEName);

		public string Description
			=> string.Format(
				"OpenInIDEDescription".GetLocalizedResource(),
				_devToolsSettingsService.FriendlyIDEName);

		public bool IsExecutable =>
			_context.Folder is not null &&
			!string.IsNullOrWhiteSpace(_devToolsSettingsService.IDEPath);

		public OpenInIDEAction()
		{
			_devToolsSettingsService = Ioc.Default.GetRequiredService<IDevToolsSettingsService>();
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
			_context.PropertyChanged += Context_PropertyChanged;
			_devToolsSettingsService.PropertyChanged += DevSettings_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return Win32Helper.RunPowershellCommandAsync(
				$"& \'{_devToolsSettingsService.IDEPath}\' \'{_context.ShellPage?.ShellViewModel.WorkingDirectory}\'",
				PowerShellExecutionOptions.Hidden
			);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.Folder))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void DevSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IDevToolsSettingsService.IDEPath))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
