// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class TogglePreviewPaneAction : ObservableObject, IToggleAction
	{
		private readonly InfoPaneViewModel viewModel;
		private readonly IInfoPaneSettingsService infoPaneSettingsService = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();

		public string Label
			=> "TogglePreviewPane".GetLocalizedResource();

		public string Description
			=> "TogglePreviewPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.PanelRight");

		public HotKey HotKey
			=> new(Keys.P, KeyModifiers.CtrlAlt);

		public bool IsOn
			=> viewModel.IsEnabled;

		public TogglePreviewPaneAction()
		{
			viewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
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
