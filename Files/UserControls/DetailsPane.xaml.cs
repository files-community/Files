using Files.Filesystem;
using Files.UserControls.FilePreviews;
using Files.ViewModels;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Files.App;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class DetailsPane : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty SelectedItemsProperty { get; } = DependencyProperty.Register("SelectedItems", typeof(List<ListedItem>), typeof(DetailsPane), new PropertyMetadata(null));
        public List<ListedItem> SelectedItems
        {
            get => (List<ListedItem>)GetValue(SelectedItemsProperty);
            set
            {
                SetValue(SelectedItemsProperty, value);

                if(value == null)
                {
                    SelectedItem = null;
                    return;
                }
                
                PreviewGrid.Children.Clear();

                if (SelectedItems.Count == 1)
                {
                    SelectedItem = SelectedItems[0];
                    SelectedItems[0].FileDetails.Clear();
                    LoadPreviewControlAsync(SelectedItems[0]);
                    return;
                }

                // Simple making the item null doesn't clear the ListView, so clear it
                SelectedItem?.FileDetails.Clear();
                SelectedItem = null;

                PreviewNotAvaliableText.Visibility = Visibility.Visible;
            }
        }


        public static DependencyProperty SelectedItemProperty { get; } = DependencyProperty.Register("SelectedItem", typeof(ListedItem), typeof(DetailsPane), new PropertyMetadata(null));
        public ListedItem SelectedItem
        {
            get => (ListedItem)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }


        private long isVerticalCallback;

        public static DependencyProperty IsHorizontalProperty { get; } = DependencyProperty.Register("IsHorizontal", typeof(bool), typeof(DetailsPane), new PropertyMetadata(null));
        public bool IsHorizontal
        {
            get => (bool)GetValue(IsHorizontalProperty);
            set => SetValue(IsHorizontalProperty, value);
        }


        // For some reason, the visual state wouldn't raise propertychangedevents with the normal property
        bool _isHorizontalInternal;
        bool isHorizontalInternal {
            get => _isHorizontalInternal;
            set
            {
                _isHorizontalInternal = value;
                RaisePropertyChanged(nameof(isHorizontalInternal));
                RaisePropertyChanged(nameof(EdgeTransitionLocation));
            }
        }

        EdgeTransitionLocation EdgeTransitionLocation { get; set; } = EdgeTransitionLocation.Left;

        Vector2 PaneSize { get; set; }

        public DetailsPane()
        {
            this.InitializeComponent();
            isVerticalCallback = RegisterPropertyChangedCallback(IsHorizontalProperty, IsVerticalChangedCallback);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }


        async void LoadPreviewControlAsync(ListedItem item)
        {
            PreviewNotAvaliableText.Visibility = Visibility.Collapsed;

            if (item.FileExtension == null)
            {
                return;
            }

            foreach (var extension in AppData.FilePreviewExtensionManager.Extensions)
            {
                if (extension.FileExtensions.Contains(item.FileExtension))
                {
                    LoadPreviewControlFromExtension(item, extension);
                    return;
                }
            }

            var control = GetBuiltInPreviewControl(item);
            if (control != null)
            {
                PreviewGrid.Children.Add(control);
                return;
            }

            control = await TextPreview.TryLoadAsTextAsync(item);
            if(control != null)
            {
                PreviewGrid.Children.Add(control);
                return;
            }

            PreviewNotAvaliableText.Visibility = Visibility.Visible;

        }

        UserControl GetBuiltInPreviewControl(ListedItem item)
        {
            var ext = item.FileExtension.ToLower();
            if (MediaPreview.Extensions.Contains(ext))
            {
                return new MediaPreview(item);
            }

            if (MarkdownPreview.Extensions.Contains(ext))
            {
                return new MarkdownPreview(item);
            }

            if (ImagePreview.Extensions.Contains(ext))
            {
                return new ImagePreview(item);
            }

            if (TextPreview.Extensions.Contains(ext))
            {
                return new TextPreview(item);
            }

            if(PDFPreview.Extensions.Contains(ext))
            {
                return new PDFPreview(item);

            }

            if (HtmlPreview.Extensions.Contains(ext))
            {
                return new HtmlPreview(item);
            }

            if (RichTextPreview.Extensions.Contains(ext))
            {
                return new RichTextPreview(item);
            }

            if(CodePreview.Extensions.Contains(ext))
            {
                return new CodePreview(item);
            }

            return null;
        }

        async void LoadPreviewControlFromExtension(ListedItem item, Helpers.Extension extension)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);

            var buffer = await FileIO.ReadBufferAsync(file);
            var byteArray = new Byte[buffer.Length];
            buffer.CopyTo(byteArray);

            try
            {
                var result = await extension.Invoke(new ValueSet() { { "byteArray", byteArray }, { "filePath", item.ItemPath } });
                var preview = result["preview"];
                PreviewGrid.Children.Add(XamlReader.Load(preview as string) as UIElement);

                var details = result["details"] as string;
                var detailsList = JsonSerializer.Deserialize<List<FileProperty>>(details);
                detailsList.ForEach(i => SelectedItem.FileDetails.Add(i));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void IsVerticalChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            isHorizontalInternal = IsHorizontal;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Checking what row the details pane is located in is a reliable way to check where the pane is
            if((int)GetValue(Grid.ColumnProperty) == 0)
            {
                EdgeTransitionLocation = EdgeTransitionLocation.Bottom;
                isHorizontalInternal = true;
            }
            else
            {
                EdgeTransitionLocation = EdgeTransitionLocation.Right;
                isHorizontalInternal = false;
            }

            RaisePropertyChanged(nameof(EdgeTransitionLocation));
        }

    }
}
