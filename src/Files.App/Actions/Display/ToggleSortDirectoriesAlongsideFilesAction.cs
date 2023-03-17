using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class ToggleSortDirectoriesAlongsideFilesAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "SettingsListAndSortDirectoriesAlongsideFiles".GetLocalizedResource();

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
