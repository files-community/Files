﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;

namespace Files.App.Actions
{
	internal sealed class OpenInVSCodeAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		private readonly bool _isVSCodeInstalled;

		public string Label
			=> "OpenInVSCode".GetLocalizedResource();

		public string Description
			=> "OpenInVSCodeDescription".GetLocalizedResource();

		public bool IsExecutable =>
			_isVSCodeInstalled &&
			_context.Folder is not null;

		public OpenInVSCodeAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_isVSCodeInstalled = SoftwareHelpers.IsVSCodeInstalled();
			if (_isVSCodeInstalled)
				_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return Win32API.RunPowershellCommandAsync($"code \'{_context.ShellPage?.FilesystemViewModel.WorkingDirectory}\'", false);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.Folder))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
