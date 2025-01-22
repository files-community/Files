// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ToggleDetailsPaneAction : ObservableObject, IToggleAction
	{
		private readonly InfoPaneViewModel viewModel;
		private readonly IInfoPaneSettingsService infoPaneSettingsService = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();

		public string Label
			=> "ToggleDetailsPane".GetLocalizedResource();

		public string Description
			=> "ToggleDetailsPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.PanelRight");

		public HotKey HotKey
			=> new(Keys.D, KeyModifiers.CtrlAlt);

		public bool IsOn
			=> viewModel.IsEnabled;

		public ToggleDetailsPaneAction()
		{
			viewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
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
