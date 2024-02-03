// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleInfoPaneAction : ObservableObject, IToggleAction
	{
		private InfoPaneViewModel InfoPaneViewModel { get; } = Ioc.Default.GetRequiredService<InfoPaneViewModel>();

		public string Label
			=> "ToggleInfoPane".GetLocalizedResource();

		public string Description
			=> "ToggleInfoPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRightPane");

		public HotKey HotKey
			=> new(Keys.I, KeyModifiers.MenuCtrl);

		public bool IsOn
			=> InfoPaneViewModel.IsEnabled;

		public ToggleInfoPaneAction()
		{
			InfoPaneViewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		}

		public Task ExecuteAsync()
		{
			InfoPaneViewModel.IsEnabled = !IsOn;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
