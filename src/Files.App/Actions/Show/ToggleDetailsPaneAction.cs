// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleDetailsPaneAction : ObservableObject, IToggleAction
	{
		private readonly InfoPaneViewModel viewModel;
		private readonly IInfoPaneSettingsService infoPaneSettingsService = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();

		public string Label
			=> "ToggleDetailsPane".GetLocalizedResource();

		public string Description
			=> "ToggleDetailsPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public HotKey HotKey
			=> new(Keys.D, KeyModifiers.MenuCtrl);

		public bool IsOn
			=> viewModel.IsEnabled;

		public ToggleDetailsPaneAction()
		{
			viewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			viewModel.IsEnabled = true;
			infoPaneSettingsService.SelectedTab = InfoPaneTabs.Details;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
