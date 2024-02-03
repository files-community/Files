// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SortFilesAndFoldersTogetherAction : ObservableObject, IToggleAction
	{
		private IDisplayPageContext DisplayContext { get; } = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label
			=> "SortFilesAndFoldersTogether".GetLocalizedResource();

		public string Description
			=> "SortFilesAndFoldersTogetherDescription".GetLocalizedResource();

		public bool IsOn
			=> DisplayContext.SortDirectoriesAlongsideFiles;

		public SortFilesAndFoldersTogetherAction()
		{
			DisplayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			DisplayContext.SortDirectoriesAlongsideFiles = true;

			return Task.CompletedTask;
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
