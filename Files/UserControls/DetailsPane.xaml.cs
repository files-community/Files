using Files.Filesystem;
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
        private DependencyProperty selectedItemsProperty = DependencyProperty.Register("SelectedItems", typeof(List<ListedItem>), typeof(DetailsPane), null);
        public List<ListedItem> SelectedItems
        {
            get => (List<ListedItem>)GetValue(selectedItemsProperty);
            set
            {
                if (value.Count == 1 && value[0].FileText != null)
                {
                    if (value[0].FileExtension.Equals(".md"))
                    {
                        MarkdownTextPreview.Text = value[0].FileText;
                        MarkdownTextPreview.Visibility = Visibility.Visible;
                        TextPreview.Visibility = Visibility.Collapsed;
                    } else
                    {
                        TextPreview.Text = value[0].FileText;
                        MarkdownTextPreview.Visibility = Visibility.Collapsed;
                        TextPreview.Visibility = Visibility.Visible;
                    }
                } else
                {
                    TextPreview.Text = "No preview avaliable";
                }
                SetValue(selectedItemsProperty, value);
            }
        }

        private bool isMarkDown = false;

        public DetailsPane()
        {
            this.InitializeComponent();
        }
    }
}
