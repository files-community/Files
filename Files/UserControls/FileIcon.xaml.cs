using Files.View_Models;
using Windows.UI.Xaml.Controls;


namespace Files.UserControls
{
    public sealed partial class FileIcon : UserControl
    {
        private SelectedItemsPropertiesViewModel viewModel;
        private double itemSize;

        public SelectedItemsPropertiesViewModel ViewModel { get => viewModel; set => viewModel = value; }
        public double ItemSize { get; set; }

        public double Size
        {
            get => itemSize;
            set
            {
                itemSize = value;
                LargerItemSize = itemSize + 2.0;
            }
        }
        private double LargerItemSize { get; set; }

        public FileIcon()
        {
            this.InitializeComponent();
        }
    }
}
