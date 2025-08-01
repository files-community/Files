// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class ToggleShelfPaneAction : ObservableObject, IToggleAction
	{
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> Strings.ToggleShelfPane.GetLocalizedResource();

		public string Description
			=> Strings.ToggleShelfPaneDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Shelf");
		
		public bool IsOn
			=> generalSettingsService.ShowShelfPane;

		public ToggleShelfPaneAction()
		{
			generalSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			generalSettingsService.ShowShelfPane = !IsOn;

			return Task.CompletedTask;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(GeneralSettingsService.ShowShelfPane))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
