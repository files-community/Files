using Files.Filesystem;
using Files.ViewModels;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class PreviewPane : UserControl
    {
        public PreviewPaneViewModel Model { get; set; } = new PreviewPaneViewModel();

        public PreviewPane()
        {
            InitializeComponent();
        }

        public List<ListedItem> SelectedItems
        {
            get => Model.SelectedItems;
            set => Model.SelectedItems = value;
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
    }
}