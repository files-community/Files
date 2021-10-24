using Files.Filesystem;
using Files.Services;
using Files.UserControls.FilePreviews;
using Files.ViewModels.Previews;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Files.ViewModels
{
    public class PreviewPaneViewModel : ObservableObject, IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private CancellationTokenSource loadCancellationTokenSource;

        private bool isItemSelected;

        public bool IsItemSelected
        {
            get => isItemSelected;
            set => SetProperty(ref isItemSelected, value);
        }

        private ListedItem selectedItem;

        public ListedItem SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }

        private PreviewPaneStates previewPaneState;

        public PreviewPaneStates PreviewPaneState
        {
            get => previewPaneState;
            set => SetProperty(ref previewPaneState, value);
        }

        private bool showCloudItemButton;

        public bool ShowCloudItemButton
        {
            get => showCloudItemButton;
            set => SetProperty(ref showCloudItemButton, value);
        }

        private UIElement previewPaneContent;

        public UIElement PreviewPaneContent
        {
            get => previewPaneContent;
            set => SetProperty(ref previewPaneContent, value);
        }

        public PreviewPaneViewModel()
        {
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
        }

        private async Task LoadPreviewControlAsync(CancellationToken token, bool downloadItem)
        {
            if (SelectedItem.IsHiddenItem)
            {
                PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;

                PreviewPaneContent = null;
                return;
            }

            var control = await GetBuiltInPreviewControlAsync(SelectedItem, downloadItem);

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (control != null)
            {
                PreviewPaneContent = control;
                PreviewPaneState = PreviewPaneStates.PreviewAndDetailsAvailable;
                return;
            }

            var basicModel = new BasicPreviewViewModel(SelectedItem);
            await basicModel.LoadAsync();
            control = new BasicPreview(basicModel);

            if (token.IsCancellationRequested)
            {
                return;
            }
            PreviewPaneContent = control;
            PreviewPaneState = PreviewPaneStates.PreviewAndDetailsAvailable;
        }

        private async Task<UserControl> GetBuiltInPreviewControlAsync(ListedItem item, bool downloadItem)
        {

            ShowCloudItemButton = false;

            if (item.IsShortcutItem)
            {
                var model = new ShortcutPreviewViewModel(SelectedItem);
                await model.LoadAsync();
                return new BasicPreview(model);
            }

            if (SelectedItem.IsZipItem)
            {
                var model = new ArchivePreviewViewModel(item);
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

            if (item.SyncStatusUI.SyncStatus == Enums.CloudDriveSyncStatus.FileOnline && !downloadItem)
            {
                ShowCloudItemButton = true;
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

            if (CodePreviewViewModel.Extensions.Contains(ext))
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
            var file = await StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath);
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

        public async void UpdateSelectedItemPreview(bool downloadItem = false)
        {
            loadCancellationTokenSource?.Cancel();
            if (SelectedItem != null && IsItemSelected)
            {
                SelectedItem?.FileDetails?.Clear();

                try
                {
                    PreviewPaneState = PreviewPaneStates.LoadingPreview;
                    loadCancellationTokenSource = new CancellationTokenSource();
                    await LoadPreviewControlAsync(loadCancellationTokenSource.Token, downloadItem);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    loadCancellationTokenSource?.Cancel();
                    // If initial loading fails, attempt to load a basic preview (thumbnail and details only)
                    // If that fails, revert to no preview/details available as long as the item is not a shortcut or folder
                    if (SelectedItem != null && !SelectedItem.IsShortcutItem && SelectedItem.PrimaryItemAttribute != StorageItemTypes.Folder)
                    {
                        try
                        {
                            var basicModel = new BasicPreviewViewModel(SelectedItem);
                            await basicModel.LoadAsync();
                            PreviewPaneContent = new BasicPreview(basicModel);
                            PreviewPaneState = PreviewPaneStates.PreviewAndDetailsAvailable;
                            return;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }

                    PreviewPaneContent = null;
                    PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;
                }
            }
            else if (IsItemSelected)
            {
                PreviewPaneContent = null;
                PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;
            }
            else
            {
                PreviewPaneContent = null;
                PreviewPaneState = PreviewPaneStates.NoItemSelected;
            }
        }

        public ICommand ShowPreviewOnlyInvoked => new RelayCommand(() => UpdateSelectedItemPreview());

        private void UserSettingsService_OnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(UserSettingsService.PreviewPaneSettingsService.ShowPreviewOnly):
                    // the preview will need refreshing as the file details won't be accurate
                    needsRefresh = true;
                    break;
            }
        }

        /// <summary>
        /// true if the content needs to be refreshed the next time the model is used
        /// </summary>
        private bool needsRefresh = false;

        /// <summary>
        /// refreshes the content if it needs to be refreshed, does nothing otherwise
        /// </summary>
        public void TryRefresh()
        {
            if (needsRefresh)
            {
                UpdateSelectedItemPreview();
            }
        }

        public void Dispose()
        {
            UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
        }
    }

    public enum PreviewPaneStates
    {
        NoItemSelected,
        NoPreviewAvailable,
        NoPreviewOrDetailsAvailable,
        PreviewAndDetailsAvailable,
        LoadingPreview,
    }
}