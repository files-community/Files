// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SortFilesFirstAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "SortFilesFirst".GetLocalizedResource();

		public string Description
			=> "SortFilesFirstDescription".GetLocalizedResource();

		public bool IsOn
			=> context.SortFilesFirst && !context.SortDirectoriesAlongsideFiles;

		public SortFilesFirstAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.SortFilesFirst = true;
			context.SortDirectoriesAlongsideFiles = false;

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortFilesFirst) or nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
