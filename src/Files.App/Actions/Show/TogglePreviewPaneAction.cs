// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class TogglePreviewPaneAction : ObservableObject, IToggleAction
	{
		private readonly InfoPaneViewModel viewModel;
		private readonly IInfoPaneSettingsService infoPaneSettingsService = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();

		public string Label
			=> "TogglePreviewPane".GetLocalizedResource();

		public string Description
			=> "TogglePreviewPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public HotKey HotKey
			=> new(Keys.P, KeyModifiers.MenuCtrl);

		public bool IsOn
			=> viewModel.IsEnabled;

		public TogglePreviewPaneAction()
		{
			viewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			viewModel.IsEnabled = true;
			infoPaneSettingsService.SelectedTab = InfoPaneTabs.Preview;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
