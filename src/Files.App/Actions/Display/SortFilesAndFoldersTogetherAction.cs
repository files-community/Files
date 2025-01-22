// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class SortFilesAndFoldersTogetherAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;
		private readonly IContentPageContext contentPageContext;

		public string Label
			=> "SortFilesAndFoldersTogether".GetLocalizedResource();

		public string Description
			=> "SortFilesAndFoldersTogetherDescription".GetLocalizedResource();

		public bool IsOn
			=> context.SortDirectoriesAlongsideFiles;

		public SortFilesAndFoldersTogetherAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();
			contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.SortDirectoriesAlongsideFiles = true;
			contentPageContext.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
