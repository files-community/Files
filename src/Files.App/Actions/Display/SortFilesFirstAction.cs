// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class SortFilesFirstAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;
		private readonly IContentPageContext contentPageContext;

		public string Label
			=> "SortFilesFirst".GetLocalizedResource();

		public string Description
			=> "SortFilesFirstDescription".GetLocalizedResource();

		public bool IsOn
			=> context.SortFilesFirst && !context.SortDirectoriesAlongsideFiles;

		public SortFilesFirstAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();
			contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.SortFilesFirst = true;
			context.SortDirectoriesAlongsideFiles = false;
			contentPageContext.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortFilesFirst) or nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
