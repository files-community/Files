// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class SortFilesAndFoldersTogetherAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> Strings.SortFilesAndFoldersTogether.GetLocalizedResource();

		public string Description
			=> Strings.SortFilesAndFoldersTogetherDescription.GetLocalizedResource();

		public bool IsOn
			=> context.SortDirectoriesAlongsideFiles;

		public SortFilesAndFoldersTogetherAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.SortDirectoriesAlongsideFiles = true;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
