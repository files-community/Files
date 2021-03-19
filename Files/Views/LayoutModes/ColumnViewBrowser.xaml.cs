using Files.EventArguments;
using Files.Filesystem;
using System;
using System.Collections;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views.LayoutModes
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColumnViewBrowser : BaseLayout
    {
        public ColumnViewBrowser()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
        }

        private void FolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
        {

        }

        public override void SelectAllItems()
        {
            // throw new NotImplementedException();
        }

        public override void FocusFileList()
        {

        }

        protected override IEnumerable GetAllItems()
        {
            return null;
        }

        protected override void AddSelectedItem(ListedItem item)
        {

        }

        public override void InvertSelection()
        {
            // throw new NotImplementedException();
        }

        public override void ClearSelection()
        {
            // throw new NotImplementedException();
        }

        public override void SetDragModeForItems()
        {
            // throw new NotImplementedException();
        }

        public override void ScrollIntoView(ListedItem item)
        {
            // throw new NotImplementedException();
        }

        public override void SetSelectedItemOnUi(ListedItem selectedItem)
        {
            // throw new NotImplementedException();
        }

        public override void SetSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            // throw new NotImplementedException();
        }

        public override void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            // throw new NotImplementedException();
        }

        public override void FocusSelectedItems()
        {
            // throw new NotImplementedException();
        }

        public override void StartRenameItem()
        {
            // throw new NotImplementedException();
        }

        public override void ResetItemOpacity()
        {
            // throw new NotImplementedException();
        }

        public override void SetItemOpacity(ListedItem item)
        {
            // throw new NotImplementedException();
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            throw new NotImplementedException();
        }
    }
}
