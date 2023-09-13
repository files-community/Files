// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to toggle preview pane visibility.
	/// </summary>
	internal class TogglePreviewPaneAction : ObservableObject, IToggleAction
	{
		private readonly PreviewPaneViewModel viewModel;

		public string Label
			=> "TogglePreviewPane".GetLocalizedResource();

		public string Description
			=> "TogglePreviewPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public HotKey HotKey
			=> new(Keys.P, KeyModifiers.Ctrl);

		public bool IsOn
			=> viewModel.IsEnabled;

		public TogglePreviewPaneAction()
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
