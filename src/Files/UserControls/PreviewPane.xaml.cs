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
        private readonly long modelChangedCallbackToken;

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model), typeof(PreviewPaneViewModel), typeof(PreviewPane), new PropertyMetadata(null));


        public PreviewPaneViewModel Model
        {
            get => (PreviewPaneViewModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        private IPaneSettingsService PaneSettingsService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

        private ObservableContext Context { get; } = new();

        public PreviewPane()
        {
            InitializeComponent();
            modelChangedCallbackToken = RegisterPropertyChangedCallback(ModelProperty, Model_DependencyPropertyChangedCallback);
        }

        private void Model_DependencyPropertyChangedCallback(DependencyObject sender, DependencyProperty dp) => Model?.TryRefresh();

        private string GetLocalized(string resName) => resName.GetLocalized();

        private void MenuFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e) => Model?.UpdateSelectedItemPreview(true);

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnregisterPropertyChangedCallback(ModelProperty, modelChangedCallbackToken);
            PreviewControlPresenter.Content = null;
            Model = null;
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