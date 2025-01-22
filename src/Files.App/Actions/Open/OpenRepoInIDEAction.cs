// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenRepoInIDEAction : ObservableObject, IAction
	{
		private readonly IDevToolsSettingsService _devToolsSettingsService;

		private readonly IContentPageContext _context;

		public string Label
			=> string.Format("OpenRepoInIDE".GetLocalizedResource(), _devToolsSettingsService.FriendlyIDEName);

		public string Description
			=> string.Format("OpenRepoInIDEDescription".GetLocalizedResource(), _devToolsSettingsService.FriendlyIDEName);

		public bool IsExecutable =>
			_context.Folder is not null &&
			_context.ShellPage!.InstanceViewModel.IsGitRepository &&
			!string.IsNullOrWhiteSpace(_devToolsSettingsService.IDEPath);

		public OpenRepoInIDEAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
			_devToolsSettingsService = Ioc.Default.GetRequiredService<IDevToolsSettingsService>();
			_context.PropertyChanged += Context_PropertyChanged;
			_devToolsSettingsService.PropertyChanged += DevSettings_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return Win32Helper.RunPowershellCommandAsync(
				$"& \'{_devToolsSettingsService.IDEPath}\' \'{_context.ShellPage!.InstanceViewModel.GitRepositoryPath}\'",
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
