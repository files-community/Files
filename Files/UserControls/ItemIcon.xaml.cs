using Files.View_Models;
using Windows.UI.Xaml.Controls;


namespace Files.UserControls
{
    public sealed partial class ItemIcon : UserControl
    {
        private SelectedItemsPropertiesViewModel viewModel;
        private double size;

        public SelectedItemsPropertiesViewModel ViewModel { get => viewModel; set => viewModel = value; }

        public double Size
        {
            get => size;
            set
            {
                size = value;
                LargerItemSize = size + 2.0;
            }
        }
        private double LargerItemSize { get; set; }

        public ItemIcon()
        {
            this.InitializeComponent();
        }
    }
}
