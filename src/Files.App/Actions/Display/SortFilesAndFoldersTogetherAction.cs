// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SortFilesAndFoldersTogetherAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "SortFilesAndFoldersTogether".GetLocalizedResource();

		public string Description
			=> "SortFilesAndFoldersTogetherDescription".GetLocalizedResource();

		public bool IsOn
			=> context.SortDirectoriesAlongsideFiles;

		public SortFilesAndFoldersTogetherAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.SortDirectoriesAlongsideFiles = true;

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
