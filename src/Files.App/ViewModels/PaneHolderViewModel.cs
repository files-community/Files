using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Filesystem;
using Files.App.Views;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using System.Collections.ObjectModel;
using System.Threading;

namespace Files.App.ViewModels
{
    public class PaneHolderViewModel : ObservableObject
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly CurrentInstanceViewModel currentInstanceViewModel;
        private readonly ToolbarViewModel toolbarViewModel;
        private readonly IDialogService dialogService;
        private readonly IUserSettingsService userSettingsService;
        private readonly IUpdateService updateService;

        private int selectedPaneIndex;

        public bool CanRefresh { get; set; }
        public bool CanNavigateToParent { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public int SelectedPaneIndex
        {
            get => selectedPaneIndex;
            set => SetProperty(ref selectedPaneIndex, value);
        }
        public bool IsMultiPaneEnabled { get; }
        public ObservableCollection<PaneNavigationArguments> Panes { get; }


        public PaneHolderViewModel(CurrentInstanceViewModel currentInstanceViewModel,
                                   ToolbarViewModel toolbarViewModel,
                                   IDialogService dialogService,
                                   IUserSettingsService userSettingsService,
                                   IUpdateService updateService)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.currentInstanceViewModel = currentInstanceViewModel;
            this.toolbarViewModel = toolbarViewModel;
            this.dialogService = dialogService;
            this.userSettingsService = userSettingsService;
            this.updateService = updateService;
            Panes = new ObservableCollection<PaneNavigationArguments>();
            //FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
            //storageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

            //this.folderSettingsViewModel.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
        }

        public void CreateNewPane(ListedItem item)
        {
            if (IsMultiPaneEnabled) // todo: detect display aspect ratio and set max panes
            {
                Panes.Add(new PaneNavigationArguments(item));

                SelectedPaneIndex = Panes.Count - 1;
            }
        }

        public enum PaneSelectionDirection
        {
            Left,
            Right
        }

        public void MovePaneSelection(PaneSelectionDirection selectionDirection)
        {
            if (SelectedPaneIndex > 0 || SelectedPaneIndex < (Panes.Count - 1))
            {
                switch (selectionDirection)
                {
                    case PaneSelectionDirection.Left:
                        SelectedPaneIndex--;
                        break;
                    case PaneSelectionDirection.Right:
                        selectedPaneIndex++;
                        break;
                }
            }
        }

        public void CloseSelectedPane()
        {
            Panes.RemoveAt(SelectedPaneIndex);
        }
    }
}
