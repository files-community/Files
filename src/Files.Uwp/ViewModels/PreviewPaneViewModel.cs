using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Services.Settings;
using Files.Shared.Cloud;
using Files.Shared.EventArguments;
using Files.Uwp.Filesystem;
using Files.Uwp.UserControls.FilePreviews;
using Files.Uwp.ViewModels.Previews;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.ViewModels
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
            ShowPreviewOnlyInvoked = new RelayCommand(() => UpdateSelectedItemPreview());

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

            if (SelectedItem.IsRecycleBinItem)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsZipItem)
                {
                    var model = new FolderPreviewViewModel(item);
                    await model.LoadAsync();
                    return new FolderPreview(model);
                }
                else
                {
                    var model = new BasicPreviewViewModel(SelectedItem);
                    await model.LoadAsync();
                    return new BasicPreview(model);
                }
            }

            if (item.IsShortcutItem)
            {
                var model = new ShortcutPreviewViewModel(SelectedItem);
                await model.LoadAsync();
                return new BasicPreview(model);
            }

            if (item.IsZipItem)
            {
                var model = new ArchivePreviewViewModel(item);
                await model.LoadAsync();
                return new BasicPreview(model);
            }

            if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                var model = new FolderPreviewViewModel(item);
                await model.LoadAsync();
                return new FolderPreview(model);
            }

            if (item.FileExtension == null)
            {
                return null;
            }

            if (item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline && !downloadItem)
            {
                ShowCloudItemButton = true;
                return null;
            }

            var ext = item.FileExtension.ToLowerInvariant();

            if (MediaPreviewViewModel.ContainsExtension(ext))
            {
                var model = new MediaPreviewViewModel(item);
                await model.LoadAsync();
                return new MediaPreview(model);
            }

            if (MarkdownPreviewViewModel.ContainsExtension(ext))
            {
                var model = new MarkdownPreviewViewModel(item);
                await model.LoadAsync();
                return new MarkdownPreview(model);
            }

            if (ImagePreviewViewModel.ContainsExtension(ext))
            {
                var model = new ImagePreviewViewModel(item);
                await model.LoadAsync();
                return new ImagePreview(model);
            }

            if (TextPreviewViewModel.ContainsExtension(ext))
            {
                var model = new TextPreviewViewModel(item);
                await model.LoadAsync();
                return new TextPreview(model);
            }

            if (PDFPreviewViewModel.ContainsExtension(ext))
            {
                var model = new PDFPreviewViewModel(item);
                await model.LoadAsync();
                return new PDFPreview(model);
            }

            if (HtmlPreviewViewModel.ContainsExtension(ext))
            {
                var model = new HtmlPreviewViewModel(item);
                await model.LoadAsync();
                return new HtmlPreview(model);
            }

            if (RichTextPreviewViewModel.ContainsExtension(ext))
            {
                var model = new RichTextPreviewViewModel(item);
                await model.LoadAsync();
                return new RichTextPreview(model);
            }

            if (CodePreviewViewModel.ContainsExtension(ext))
            {
                var model = new CodePreviewViewModel(item);
                await model.LoadAsync();
                return new CodePreview(model);
            }

            var control = await TextPreviewViewModel.TryLoadAsTextAsync(item);
            if (control != null)
            {
                return control;
            }

            return null;
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
                        await LoadBasicPreviewAsync();
                        return;
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

        public ICommand ShowPreviewOnlyInvoked { get; }

        private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            if (e.SettingName == nameof(IPaneSettingsService.ShowPreviewOnly))
            {
                // The preview will need refreshing as the file details won't be accurate
                needsRefresh = true;
            }
        }

        private async Task LoadBasicPreviewAsync()
        {
            try
            {
                var basicModel = new BasicPreviewViewModel(SelectedItem);
                await basicModel.LoadAsync();
                PreviewPaneContent = new BasicPreview(basicModel);
                PreviewPaneState = PreviewPaneStates.PreviewAndDetailsAvailable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
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