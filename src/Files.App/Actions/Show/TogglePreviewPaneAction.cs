using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class TogglePreviewPaneAction : ObservableObject, IToggleAction
	{
		private readonly PreviewPaneViewModel viewModel;

		public string Label { get; } = "TogglePreviewPane".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconRightPane");
		public HotKey HotKey { get; } = new(Keys.P, KeyModifiers.Ctrl);

		public bool IsOn => viewModel.IsEnabled;

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
