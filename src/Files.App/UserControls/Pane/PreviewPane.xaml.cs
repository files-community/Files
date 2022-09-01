using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.ViewModels;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls
{
	public sealed partial class PreviewPane : UserControl, IPane
	{
		public PanePositions Position { get; private set; } = PanePositions.None;

		private IPaneSettingsService PaneSettingsService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

		private PreviewPaneViewModel ViewModel => App.PreviewPaneViewModel;

		private ObservableContext Context { get; } = new();

		public PreviewPane() => InitializeComponent();

		public void UpdatePosition(double panelWidth, double panelHeight)
		{
			if (panelWidth > 700)
			{
				Position = PanePositions.Right;
				(MinWidth, MinHeight) = (150, 0);
			}
			else
			{
				Position = PanePositions.Bottom;
				(MinWidth, MinHeight) = (0, 140);
			}
		}

		private string GetLocalizedResource(string resName) => resName.GetLocalizedResource();

		private void Root_Loading(FrameworkElement sender, object args)
			=> ViewModel.UpdateSelectedItemPreview();
		private void Root_Unloaded(object sender, RoutedEventArgs e)
		{
			PreviewControlPresenter.Content = null;
			Bindings.StopTracking();
		}
		private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
			=> Context.IsHorizontal = Root.ActualWidth >= Root.ActualHeight;

		private void MenuFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel?.UpdateSelectedItemPreview(true);

		private class ObservableContext : ObservableObject
		{
			private bool isHorizontal = false;
			public bool IsHorizontal
			{
				get => isHorizontal;
				set => SetProperty(ref isHorizontal, value);
			}
		}
	}
}