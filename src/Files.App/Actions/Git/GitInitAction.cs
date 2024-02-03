// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	sealed class GitInitAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "InitRepo".GetLocalizedResource();

		public string Description
			=> "InitRepoDescription".GetLocalizedResource();

		public bool IsExecutable => 
			ContentPageContext.Folder is not null &&
			ContentPageContext.Folder.ItemPath != SystemIO.Path.GetPathRoot(ContentPageContext.Folder.ItemPath) &&
			!ContentPageContext.IsGitRepository;

		public GitInitAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return GitHelpers.InitializeRepositoryAsync(ContentPageContext.Folder?.ItemPath);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.IsGitRepository):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
