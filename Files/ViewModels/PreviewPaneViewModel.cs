using Files.Filesystem;
using Files.UserControls.FilePreviews;
using Files.ViewModels.Previews;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using static Files.App;

namespace Files.ViewModels
{
    public class PreviewPaneViewModel : ObservableObject
    {
        private List<ListedItem> selectedItems;

        public List<ListedItem> SelectedItems
        {
            get => selectedItems;
            set
            {
                if (SetProperty(ref selectedItems, value))
                {
                    SelectedItem = SelectedItems?.FirstOrDefault();
                }
            }
        }

        private ListedItem selectedItem;

        private CancellationTokenSource loadCancellationTokenSource;

        public ListedItem SelectedItem
        {
            get => selectedItem;
            set
            {
                SetProperty(ref selectedItem, value);
                SelectedItem?.FileDetails?.Clear();
                SelectedItemChanged();
            }
        }

        private string previewErrorText;

        public string PreviewErrorText
        {
            get => previewErrorText;
            set => SetProperty(ref previewErrorText, value);
        }

        private string detailsErrorText;

        public string DetailsErrorText
        {
            get => detailsErrorText;
            set => SetProperty(ref detailsErrorText, value);
        }

        private Visibility detailsListVisibility = Visibility.Collapsed;

        public Visibility DetailsListVisibility
        {
            get => detailsListVisibility;
            set => SetProperty(ref detailsListVisibility, value);
        }

        private UIElement previewPaneContent;

        public UIElement PreviewPaneContent
        {
            get => previewPaneContent;
            set => SetProperty(ref previewPaneContent, value);
        }

        public PreviewPaneViewModel()
        {
        }

        private async Task LoadPreviewControlAsync(CancellationToken token)
        {
            DetailsErrorText = "";
            PreviewErrorText = "";

            if (SelectedItem.IsHiddenItem)
            {
                DetailsErrorText = "PreviewPaneDetailsNotAvailableText".GetLocalized();
                PreviewErrorText = "DetailsPanePreviewNotAvaliableText".GetLocalized();
                PreviewPaneContent = null;
                return;
            }

            foreach (var extension in AppData.FilePreviewExtensionManager.Extensions)
            {
                if (extension.FileExtensions.Contains(SelectedItem.FileExtension))
                {
                    var extControl = await LoadPreviewControlFromExtension(SelectedItem, extension);
                    if (!token.IsCancellationRequested && extControl != null)
                    {
                        PreviewPaneContent = extControl;
                    }

                    return;
                }
            }

            var control = await GetBuiltInPreviewControlAsync(SelectedItem);

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (control != null)
            {
                PreviewPaneContent = control;
                return;
            }

            var basicModel = new BasicPreviewViewModel(SelectedItem);
            await basicModel.LoadAsync();
            control = new BasicPreview(basicModel);
            PreviewPaneContent = control;
        }

        private async Task<UserControl> GetBuiltInPreviewControlAsync(ListedItem item)
        {
            if (item.IsShortcutItem)
            {
                var model = new ShortcutPreviewViewModel(SelectedItem);
                await model.LoadAsync();
                return new BasicPreview(model);
            }

            if (SelectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                var model = new FolderPreviewViewModel(SelectedItem);
                await model.LoadAsync();
                return new FolderPreview(model);
            }

            if (item.FileExtension == null)
            {
                return null;
            }

            var ext = item.FileExtension.ToLower();
            if (MediaPreviewViewModel.Extensions.Contains(ext))
            {
                var model = new MediaPreviewViewModel(item);
                await model.LoadAsync();
                return new MediaPreview(model);
            }

            if (MarkdownPreviewViewModel.Extensions.Contains(ext))
            {
                var model = new MarkdownPreviewViewModel(item);
                await model.LoadAsync();
                return new MarkdownPreview(model);
            }

            if (ImagePreviewViewModel.Extensions.Contains(ext))
            {
                var model = new ImagePreviewViewModel(item);
                await model.LoadAsync();
                return new ImagePreview(model);
            }

            if (TextPreviewViewModel.Extensions.Contains(ext))
            {
                var model = new TextPreviewViewModel(item);
                await model.LoadAsync();
                return new TextPreview(model);
            }

            if (PDFPreviewViewModel.Extensions.Contains(ext))
            {
                var model = new PDFPreviewViewModel(item);
                await model.LoadAsync();
                return new PDFPreview(model);
            }

            if (HtmlPreviewViewModel.Extensions.Contains(ext))
            {
                var model = new HtmlPreviewViewModel(item);
                await model.LoadAsync();
                return new HtmlPreview(model);
            }

            if (RichTextPreviewViewModel.Extensions.Contains(ext))
            {
                var model = new RichTextPreviewViewModel(item);
                await model.LoadAsync();
                return new RichTextPreview(model);
            }

            if(CodePreviewViewModel.Extensions.Contains(ext))
            {
                var model = new CodePreviewViewModel(item);
                await model.LoadAsync();
                return new CodePreview(model);
            }

            var control = await TextPreviewViewModel.TryLoadAsTextAsync(SelectedItem);
            if (control != null)
            {
                return control;
            }

            return null;
        }

        private async Task<UIElement> LoadPreviewControlFromExtension(ListedItem item, Extension extension)
        {
            UIElement control = null;
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
            string sharingToken = SharedStorageAccessManager.AddFile(file);
            var result = await extension.Invoke(new ValueSet() { { "token", sharingToken } });

            if (result.TryGetValue("preview", out object preview))
            {
                control = XamlReader.Load(preview as string) as UIElement;
            }

            if (result.TryGetValue("details", out object details))
            {
                var detailsList = JsonConvert.DeserializeObject<List<FileProperty>>(details as string);
                await BasePreviewModel.LoadDetailsOnly(item, detailsList);
            }

            return control;
        }

        private async void SelectedItemChanged()
        {
            loadCancellationTokenSource?.Cancel();
            if (SelectedItem != null && SelectedItems.Count == 1)
            {
                try
                {
                    DetailsListVisibility = Visibility.Visible;
                    loadCancellationTokenSource = new CancellationTokenSource();
                    await LoadPreviewControlAsync(loadCancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    loadCancellationTokenSource?.Cancel();
                    // If initial loading fails, attempt to load a basic preivew (thumbnail and details only)
                    // If that fails, revert to no preview/details available as long as the item is not a shortcut or folder
                    if(!SelectedItem.IsShortcutItem && SelectedItem.PrimaryItemAttribute != StorageItemTypes.Folder)
                    {
                        try
                        {
                            var basicModel = new BasicPreviewViewModel(SelectedItem);
                            await basicModel.LoadAsync();
                            PreviewPaneContent = new BasicPreview(basicModel);
                            return;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }

                    PreviewPaneContent = null;
                    DetailsListVisibility = Visibility.Collapsed;
                    DetailsErrorText = "PreviewPaneDetailsNotAvailableText".GetLocalized();
                    PreviewErrorText = "DetailsPanePreviewNotAvaliableText".GetLocalized();
                }
            }
            else if (SelectedItem != null)
            {
                PreviewPaneContent = null;
                DetailsErrorText = "PreviewPaneDetailsNotAvailableText".GetLocalized();
                PreviewErrorText = "DetailsPanePreviewNotAvaliableText".GetLocalized();
                DetailsListVisibility = Visibility.Collapsed;
            }
            else
            {
                PreviewPaneContent = null;
                DetailsErrorText = "NoItemSelected".GetLocalized();
                PreviewErrorText = "NoItemSelected".GetLocalized();
                DetailsListVisibility = Visibility.Collapsed;
            }
        }
    }
}