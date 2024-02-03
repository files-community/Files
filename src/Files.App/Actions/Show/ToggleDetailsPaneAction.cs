// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleDetailsPaneAction : ObservableObject, IToggleAction
	{
		private InfoPaneViewModel InfoPaneViewModel { get; } = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		private IInfoPaneSettingsService InfoPaneSettingsService { get; } = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();

		public string Label
			=> "ToggleDetailsPane".GetLocalizedResource();

		public string Description
			=> "ToggleDetailsPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public HotKey HotKey
			=> new(Keys.D, KeyModifiers.MenuCtrl);

		public bool IsOn
			=> InfoPaneViewModel.IsEnabled;

		public ToggleDetailsPaneAction()
		{
		}

		public Task ExecuteAsync()
		{
			InfoPaneViewModel.IsEnabled = true;
			InfoPaneSettingsService.SelectedTab = InfoPaneTabs.Details;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
