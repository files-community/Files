using Microsoft.Toolkit.Uwp;
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
    public sealed partial class FolderEmptyIndicator : UserControl
    {
        public EmptyTextType EmptyTextType
        {
            get { return (EmptyTextType)GetValue(EmptyTextTypeProperty); }
            set { SetValue(EmptyTextTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EmptyTextType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EmptyTextTypeProperty =
            DependencyProperty.Register("EmptyTextType", typeof(EmptyTextType), typeof(FolderEmptyIndicator), new PropertyMetadata(null));

        private string GetTranslated(string resourceName) => resourceName.GetLocalized();

        public FolderEmptyIndicator()
        {
            this.InitializeComponent();
        }
    }

    public enum EmptyTextType
    {
        None,
        FolderEmpty,
        NoSearchResultsFound,
    }
}
