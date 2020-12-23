using Files.View_Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class DetailsPane : UserControl
    {
        private DependencyProperty selectedItemsPropertiesViewModelProperty = DependencyProperty.Register("SelectedItemsPropertiesViewModel", typeof(SelectedItemsPropertiesViewModel), typeof(DetailsPane), null);
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel
        {
            get => (SelectedItemsPropertiesViewModel) GetValue(selectedItemsPropertiesViewModelProperty);
            set => SetValue(selectedItemsPropertiesViewModelProperty, value);
        }

        public DetailsPane()
        {
            this.InitializeComponent();
        }
    }
}
