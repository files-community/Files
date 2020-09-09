using Files.View_Models;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class FileIcon : UserControl
    {
        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        private double itemSize;
        public double ItemSize
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
