// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleInfoPaneAction : ObservableObject, IToggleAction
	{
		private readonly InfoPaneViewModel viewModel;

		public string Label
			=> "ToggleInfoPane".GetLocalizedResource();

		public string Description
			=> "ToggleInfoPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public HotKey HotKey
			=> new(Keys.I, KeyModifiers.MenuCtrl);

		public bool IsOn
			=> viewModel.IsEnabled;

		public ToggleInfoPaneAction()
		{
			viewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			viewModel.IsEnabled = !IsOn;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
