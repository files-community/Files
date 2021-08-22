using Files.Filesystem;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class PreviewPane : UserControl
    {


        public PreviewPaneViewModel Model
        {
            get => (PreviewPaneViewModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model), typeof(PreviewPaneViewModel), typeof(PreviewPane), new PropertyMetadata(null));

        public PreviewPane()
        {
            InitializeComponent();
        }

        public static DependencyProperty IsHorizontalProperty { get; } =
            DependencyProperty.Register("IsHorizontal", typeof(bool), typeof(PreviewPane), new PropertyMetadata(null));

        public bool IsHorizontal
        {
            get => (bool)GetValue(IsHorizontalProperty);
            set
            {
                SetValue(IsHorizontalProperty, value);
                EdgeTransitionLocation = value ? EdgeTransitionLocation.Bottom : EdgeTransitionLocation.Right;
            }
        }

        public static DependencyProperty EdgeTransitionLocationProperty =
            DependencyProperty.Register("EdgeTransitionLocation",
                                        typeof(EdgeTransitionLocation),
                                        typeof(PreviewPane),
                                        new PropertyMetadata(null));

        private EdgeTransitionLocation EdgeTransitionLocation
        {
            get => (EdgeTransitionLocation)GetValue(EdgeTransitionLocationProperty);
            set => SetValue(EdgeTransitionLocationProperty, value);
        }

        private string GetLocalizedText(string resName) => resName.GetLocalized();

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
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