using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.Backend.Services.Settings;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class ShowFileExtensionsAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public string Label => "ShowFileExtensions".GetLocalizedResource();

		public bool IsOn => settings.ShowFileExtensions;

		public ShowFileExtensionsAction() => settings.PropertyChanged += Settings_PropertyChanged;

		public Task ExecuteAsync()
		{
			settings.ShowFileExtensions = !settings.ShowFileExtensions;
			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowFileExtensions))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
