﻿using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class GitPullAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		public string Label { get; } = "GitPull".GetLocalizedResource();

		public string Description { get; } = "GitPullDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE74B");

		public bool IsExecutable
			=> _context.CanExecuteGitAction;

		public GitPullAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			GitHelpers.PullOrigin(_context.ShellPage!.InstanceViewModel.GitRepositoryPath);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
