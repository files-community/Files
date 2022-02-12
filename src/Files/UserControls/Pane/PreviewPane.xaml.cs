using Files.Services;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class PreviewPane : UserControl
    {
        private IPaneSettingsService PaneSettingsService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

        private PreviewPaneViewModel ViewModel => App.PreviewPaneViewModel;

        private ObservableContext Context { get; } = new();

        public PreviewPane() => InitializeComponent();

        private string GetLocalized(string resName) => resName.GetLocalized();

        private void MenuFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel?.UpdateSelectedItemPreview(true);

        private void Root_Loading(FrameworkElement sender, object args)
        {
            ViewModel.UpdateSelectedItemPreview();
        }
        private void Root_Unloaded(object sender, RoutedEventArgs e)
        {
            PreviewControlPresenter.Content = null;
            Bindings.StopTracking();
        }
        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
            => Context.IsHorizontal = Root.ActualWidth >= Root.ActualHeight;

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