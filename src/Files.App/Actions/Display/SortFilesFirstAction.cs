// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SortFilesFirstAction : ObservableObject, IToggleAction
	{
		private IDisplayPageContext DisplayContext { get; } = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label
			=> "SortFilesFirst".GetLocalizedResource();

		public string Description
			=> "SortFilesFirstDescription".GetLocalizedResource();

		public bool IsOn
			=> DisplayContext.SortFilesFirst && !DisplayContext.SortDirectoriesAlongsideFiles;

		public SortFilesFirstAction()
		{
			DisplayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			DisplayContext.SortFilesFirst = true;
			DisplayContext.SortDirectoriesAlongsideFiles = false;

			return Task.CompletedTask;
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortFilesFirst) or nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
