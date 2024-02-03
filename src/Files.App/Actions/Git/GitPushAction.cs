﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class GitPushAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		public string Label { get; } = "Push".GetLocalizedResource();

		public string Description { get; } = "GitPushDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE74A");

		public bool IsExecutable =>
			_context.CanExecuteGitAction;

		public GitPushAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return GitHelpers.PushToOriginAsync(
				_context.ShellPage?.InstanceViewModel.GitRepositoryPath,
				_context.ShellPage?.InstanceViewModel.GitBranchName);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
