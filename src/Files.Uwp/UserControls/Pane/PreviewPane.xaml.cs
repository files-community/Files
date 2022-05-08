using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.Uwp.UserControls
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
                (MinWidth, MaxWidth, MinHeight, MaxHeight) = (150, 500, 0, double.MaxValue);
            }
            else
            {
                Position = PanePositions.Bottom;
                (MinWidth, MaxWidth, MinHeight, MaxHeight) = (0, double.MaxValue, 140, double.MaxValue);
            }
        }

        private string GetLocalized(string resName) => resName.GetLocalized();

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