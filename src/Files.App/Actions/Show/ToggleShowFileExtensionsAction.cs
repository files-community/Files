using Files.Backend.Services.Settings;

namespace Files.App.Actions
{
	internal class ToggleShowFileExtensionsAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public string Label { get; } = "ShowFileExtensions".GetLocalizedResource();

		public string Description => "ToggleShowFileExtensionsDescription".GetLocalizedResource();

		public bool IsOn => settings.ShowFileExtensions;

		public ToggleShowFileExtensionsAction() => settings.PropertyChanged += Settings_PropertyChanged;

		public Task ExecuteAsync()
		{
			settings.ShowFileExtensions = !settings.ShowFileExtensions;
			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowFileExtensions))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
