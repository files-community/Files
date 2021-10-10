using Files.Services;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class PreviewPane : UserControl
    {
        public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public PreviewPaneViewModel Model
        {
            get => (PreviewPaneViewModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model), typeof(PreviewPaneViewModel), typeof(PreviewPane), new PropertyMetadata(null));

        private long modelChangedCallbackToken;

        public PreviewPane()
        {
            InitializeComponent();
            modelChangedCallbackToken = RegisterPropertyChangedCallback(ModelProperty, Model_DependencyPropertyChangedCallback);
        }

        private void Model_DependencyPropertyChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            // try to refresh the model when the instance is switched
            Model?.TryRefresh();
        }

        public static DependencyProperty IsHorizontalProperty { get; } =
            DependencyProperty.Register("IsHorizontal", typeof(bool), typeof(PreviewPane), new PropertyMetadata(null));

        public bool IsHorizontal
        {
            get => (bool)GetValue(IsHorizontalProperty);
            set
            {
                SetValue(IsHorizontalProperty, value);
            }
        }

        private string GetLocalizedText(string resName) => resName.GetLocalized();

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnregisterPropertyChangedCallback(ModelProperty, modelChangedCallbackToken);
            PreviewControlPresenter.Content = null;
            Model = null;
            this.Bindings.StopTracking();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Model?.UpdateSelectedItemPreview(true);
        }
    }
}