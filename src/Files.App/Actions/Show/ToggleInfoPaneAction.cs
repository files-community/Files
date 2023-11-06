// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleInfoPaneAction : ObservableObject, IToggleAction
	{
		private readonly PreviewPaneViewModel viewModel;
		private readonly IPreviewPaneSettingsService previewSettingsService = Ioc.Default.GetRequiredService<IPreviewPaneSettingsService>();

		public string Label
			=> "ToggleInfoPane".GetLocalizedResource();

		public string Description
			=> "ToggleInfoDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public bool IsOn
			=> viewModel.IsEnabled;

		public ToggleInfoPaneAction()
		{
			viewModel = Ioc.Default.GetRequiredService<PreviewPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			viewModel.IsEnabled = !IsOn;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(PreviewPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
