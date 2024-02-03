// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenRepoInVSCodeAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly bool _isVSCodeInstalled;

		public string Label
			=> "OpenRepoInVSCode".GetLocalizedResource();

		public string Description
			=> "OpenRepoInVSCodeDescription".GetLocalizedResource();

		public bool IsExecutable =>
			_isVSCodeInstalled &&
			ContentPageContext.Folder is not null &&
			ContentPageContext.ShellPage!.InstanceViewModel.IsGitRepository;

		public OpenRepoInVSCodeAction()
		{
			ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			_isVSCodeInstalled = SoftwareHelpers.IsVSCodeInstalled();
			if (_isVSCodeInstalled)
				ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return Win32API.RunPowershellCommandAsync($"code \'{ContentPageContext.ShellPage!.InstanceViewModel.GitRepositoryPath}\'", false);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.Folder))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
