// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class TogglePreviewPaneAction : ObservableObject, IToggleAction
	{
		private InfoPaneViewModel InfoPaneViewModel { get; } = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		private IInfoPaneSettingsService InfoPaneSettingsService { get; } = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();

		public string Label
			=> "TogglePreviewPane".GetLocalizedResource();

		public string Description
			=> "TogglePreviewPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public HotKey HotKey
			=> new(Keys.P, KeyModifiers.MenuCtrl);

		public bool IsOn
			=> InfoPaneViewModel.IsEnabled;

		public TogglePreviewPaneAction()
		{
			InfoPaneViewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
			InfoPaneViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			InfoPaneViewModel.IsEnabled = true;
			InfoPaneSettingsService.SelectedTab = InfoPaneTabs.Preview;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
