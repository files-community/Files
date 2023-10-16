// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;

namespace Files.App.Actions
{
	internal sealed class OpenRepoInVSCodeAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		private readonly bool _isVSCodeInstalled;

		public string Label
			=> "OpenRepoInVSCode".GetLocalizedResource();

		public string Description
			=> "OpenRepoInVSCodeDescription".GetLocalizedResource();

		public bool IsExecutable =>
			_isVSCodeInstalled &&
			_context.Folder is not null &&
			_context.ShellPage!.InstanceViewModel.IsGitRepository;

		public OpenRepoInVSCodeAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_isVSCodeInstalled = SoftwareHelpers.IsVSCodeInstalled();
			if (_isVSCodeInstalled)
				_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return Win32API.RunPowershellCommandAsync($"code \'{_context.ShellPage!.InstanceViewModel.GitRepositoryPath}\'", false);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.Folder))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
