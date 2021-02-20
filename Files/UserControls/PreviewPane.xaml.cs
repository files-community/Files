using Files.Filesystem;
using Files.UserControls.FilePreviews;
using Files.ViewModels.Previews;
using Files.ViewModels.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using static Files.App;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class PreviewPane : UserControl
    {
        public PreviewPane()
        {
            InitializeComponent();

            RegisterPropertyChangedCallback(Grid.RowProperty, GridRowChangedCallback);
        }

        public static DependencyProperty SelectedItemsProperty { get; } =
            DependencyProperty.Register("SelectedItems", typeof(List<ListedItem>), typeof(PreviewPane), new PropertyMetadata(null));

        public List<ListedItem> SelectedItems
        {
            get => (List<ListedItem>)GetValue(SelectedItemsProperty);
            set
            {
                SetValue(SelectedItemsProperty, value);

                if (value == null)
                {
                    SelectedItem = null;
                    return;
                }

                PreviewGrid.Children.Clear();
                previewPaneLoadingCancellationTokenSource?.Cancel();

                if (SelectedItems.Count == 1)
                {
                    SelectedItem = SelectedItems[0];
                    SelectedItem.FileDetails?.Clear();
                    previewPaneLoadingCancellationTokenSource = new CancellationTokenSource();
                    LoadPreviewControlAsync(SelectedItems[0], previewPaneLoadingCancellationTokenSource);
                    return;
                }

                // Making the item null doesn't clear the ListView, so clear it
                SelectedItem?.FileDetails?.Clear();
                SelectedItem = null;

                PreviewNotAvaliableText.Visibility = Visibility.Visible;
                PreviewPaneDetailsNotAvailableText.Visibility = Visibility.Visible;
            }
        }

        public static DependencyProperty SelectedItemProperty { get; } =
            DependencyProperty.Register("SelectedItem", typeof(ListedItem), typeof(PreviewPane), new PropertyMetadata(null));

        public ListedItem SelectedItem
        {
            get => (ListedItem)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private CancellationTokenSource previewPaneLoadingCancellationTokenSource;

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

        private async void LoadPreviewControlAsync(ListedItem item, CancellationTokenSource cancellationTokenSource)
        {
            PreviewNotAvaliableText.Visibility = Visibility.Collapsed;
            PreviewPaneDetailsNotAvailableText.Visibility = Visibility.Collapsed;

            // Folders and shortcuts are not supported yet
            if (item.FileExtension == null || item.IsShortcutItem)
            {
                PreviewNotAvaliableText.Visibility = Visibility.Visible;
                PreviewPaneDetailsNotAvailableText.Visibility = Visibility.Visible;
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

            control = await TextPreviewViewModel.TryLoadAsTextAsync(item);
            if (control != null)
            {
                PreviewGrid.Children.Add(control);
                return;
            }

            // Exit if the loading has been cancelled since the function was run
            // prevents duplicate loading
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            BasePreviewModel.LoadDetailsOnly(item);

            PreviewNotAvaliableText.Visibility = Visibility.Visible;
        }

        private UserControl GetBuiltInPreviewControl(ListedItem item)
        {
            var ext = item.FileExtension.ToLower();
            if (MediaPreviewViewModel.Extensions.Contains(ext))
            {
                return new MediaPreview(new MediaPreviewViewModel(item));
            }

            if (MarkdownPreviewViewModel.Extensions.Contains(ext))
            {
                return new MarkdownPreview(new MarkdownPreviewViewModel(item));
            }

            if (ImagePreviewViewModel.Extensions.Contains(ext))
            {
                return new ImagePreview(new ImagePreviewViewModel(item));
            }

            if (TextPreviewViewModel.Extensions.Contains(ext))
            {
                return new TextPreview(new TextPreviewViewModel(item));
            }

            if (PDFPreviewViewModel.Extensions.Contains(ext))
            {
                return new PDFPreview(new PDFPreviewViewModel(item));
            }

            if (HtmlPreviewViewModel.Extensions.Contains(ext))
            {
                return new HtmlPreview(new HtmlPreviewViewModel(item));
            }

            if (RichTextPreviewViewModel.Extensions.Contains(ext))
            {
                return new RichTextPreview(new RichTextPreviewViewModel(item));
            }

            //if (CodePreviewViewModel.Extensions.Contains(ext))
            //{
            //    return new CodePreview(new CodePreviewViewModel(item));
            //}

            return null;
        }

        private async void LoadPreviewControlFromExtension(ListedItem item, Extension extension)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
                string sharingToken = SharedStorageAccessManager.AddFile(file);
                var result = await extension.Invoke(new ValueSet() { { "token", sharingToken } });

                if (result.TryGetValue("preview", out object preview))
                {
                    PreviewGrid.Children.Add(XamlReader.Load(preview as string) as UIElement);
                }

                if (result.TryGetValue("details", out object details))
                {
                    var detailsList = JsonConvert.DeserializeObject<List<FileProperty>>(details as string);
                    BasePreviewModel.LoadDetailsOnly(item, detailsList);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void GridRowChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            UpdatePreviewLayout();
        }

        private void UpdatePreviewLayout()
        {
            // Checking what row the details pane is located in is a reliable way to check where the pane is
            if ((int)GetValue(Grid.ColumnProperty) == 0)
            {
                EdgeTransitionLocation = EdgeTransitionLocation.Bottom;
                IsHorizontal = true;
            }
            else
            {
                EdgeTransitionLocation = EdgeTransitionLocation.Right;
                IsHorizontal = false;
            }
        }

        private void UserControl_Loading(FrameworkElement sender, object args)
        {
            UpdatePreviewLayout();
        }
    }
}