using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.Core.Services.Settings;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class ToggleShowHiddenItemsAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public string Label { get; } = "ShowHiddenItems".GetLocalizedResource();

		public bool IsOn => settings.ShowHiddenItems;

		public ToggleShowHiddenItemsAction() => settings.PropertyChanged += Settings_PropertyChanged;

		public Task ExecuteAsync()
		{
			settings.ShowHiddenItems = !settings.ShowHiddenItems;
			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowHiddenItems))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
