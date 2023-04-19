using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class ToggleSortDirectoriesAlongsideFilesAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "SettingsListAndSortDirectoriesAlongsideFiles".GetLocalizedResource();

		public string Description => "ToggleSortDirectoriesAlongsideFilesDescription".GetLocalizedResource();

		public bool IsOn => context.SortDirectoriesAlongsideFiles;

		public ToggleSortDirectoriesAlongsideFilesAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.SortDirectoriesAlongsideFiles = !IsOn;
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
