using Files.Filesystem;
using Files.UserControls.FilePreviews;
using Files.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
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
                PreviewGrid.Children.Clear();
                PreviewNotAvaliableText.Visibility = Visibility.Visible;

                if (value.Count == 1)
                {
                    if (TryLoadPreviewControl(value[0]))
                    {
                        PreviewNotAvaliableText.Visibility = Visibility.Collapsed;
                    }
                }
            }
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
            } 
        }

        public DetailsPane()
        {
            this.InitializeComponent();
            isVerticalCallback = RegisterPropertyChangedCallback(IsHorizontalProperty, isVerticalChangedCallback);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        bool TryLoadPreviewControl(ListedItem item)
        {
            foreach (var extension in AppData.FilePreviewExtensionManager.Extensions)
            {
                if (extension.FileExtensions.Contains(item.FileExtension))
                {
                    LoadPreviewControlFromExtension(item, extension);
                    return true;
                }
            }

            var control = GetBuiltInPreviewControl(item);
            if (control != null)
            {
                //control.HorizontalAlignment = HorizontalAlignment.Stretch;
                //control.VerticalAlignment = VerticalAlignment.Stretch;
                PreviewGrid.Children.Add(control);
                return true;
            }

            return false;
        }

        UserControl GetBuiltInPreviewControl(ListedItem item)
        {
            if (MediaPreview.Extensions.Contains(item.FileExtension))
            {
                return new MediaPreview(item.ItemPath);
            }

            if (MarkdownPreview.Extensions.Contains(item.FileExtension))
            {
                return new MarkdownPreview(item.ItemPath);
            }

            if (ImagePreview.Extensions.Contains(item.FileExtension))
            {
                return new ImagePreview(item.ItemPath);
            }

            if (TextPreview.Extensions.Contains(item.FileExtension))
            {
                return new TextPreview(item.ItemPath);
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
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void isVerticalChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            isHorizontalInternal = IsHorizontal;
        }
    }
}
