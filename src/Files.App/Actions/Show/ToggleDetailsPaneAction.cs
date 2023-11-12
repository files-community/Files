// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleDetailsPaneAction : ObservableObject, IToggleAction
	{
		private readonly PreviewPaneViewModel viewModel;
		private readonly IPreviewPaneSettingsService previewSettingsService = Ioc.Default.GetRequiredService<IPreviewPaneSettingsService>();

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
			viewModel = Ioc.Default.GetRequiredService<PreviewPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			viewModel.IsEnabled = true;
			previewSettingsService.ShowPreviewOnly = false;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(PreviewPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
