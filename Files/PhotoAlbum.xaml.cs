using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Filesystem;
using Windows.UI.Xaml.Input;
using Windows.System;
using Interaction = Files.Interacts.Interaction;
using Windows.UI.Core;

namespace Files
{

    public sealed partial class PhotoAlbum : BaseLayout
    {

        public PhotoAlbum()
        {
            this.InitializeComponent();

        }

        private void FileList_Tapped(object sender, TappedRoutedEventArgs e)
        {

            var BoxPressed = Interaction.FindParent<GridViewItem>(e.OriginalSource as DependencyObject);
            if (BoxPressed == null)
            {
                 FileList.SelectedItems.Clear();
            }
        }

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var parentContainer = Interaction.FindParent<GridViewItem>(e.OriginalSource as DependencyObject);
            if (FileList.SelectedItems.Contains(FileList.ItemFromContainer(parentContainer)))
            {
                return;
            }
            // The following code is only reachable when a user RightTapped an unselected row
            FileList.SelectedItems.Clear();
            FileList.SelectedItems.Add(FileList.ItemFromContainer(parentContainer) as ListedItem);
        }

        private void PhotoAlbumViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Page).Properties.IsLeftButtonPressed)
            {
                FileList.SelectedItem = null;
                App.OccupiedInstance.HomeItems.isEnabled = false;
                App.OccupiedInstance.ShareItems.isEnabled = false;
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                App.OccupiedInstance.HomeItems.isEnabled = true;
                App.OccupiedInstance.ShareItems.isEnabled = true;

            }
            else if (FileList.SelectedItems.Count == 0)
            {
                App.OccupiedInstance.HomeItems.isEnabled = false;
                App.OccupiedInstance.ShareItems.isEnabled = false;
            }
        }

        private ListedItem renamingItem;

        public void StartRename()
        {
            renamingItem = FileList.SelectedItem as ListedItem;
            GridViewItem gridViewItem = FileList.ContainerFromItem(renamingItem) as GridViewItem;
            StackPanel stackPanel = (gridViewItem.ContentTemplateRoot as Grid).Children[1] as StackPanel;
            TextBlock textBlock = stackPanel.Children[0] as TextBlock;
            TextBox textBox = stackPanel.Children[1] as TextBox;
            int extensionLength = renamingItem.DotFileExtension?.Length ?? 0;

            textBlock.Visibility = Visibility.Collapsed;
            textBox.Visibility = Visibility.Visible;
            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;
            textBox.Select(0, renamingItem.FileName.Length - extensionLength);
            isRenamingItem = true;
        }

        private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                TextBox textBox = sender as TextBox;
                textBox.LostFocus -= RenameTextBox_LostFocus;
                EndRename(textBox);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter)
            {
                TextBox textBox = sender as TextBox;
                CommitRename(textBox);
                e.Handled = true;
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = e.OriginalSource as TextBox;
            CommitRename(textBox);
        }

        private async void CommitRename(TextBox textBox)
        {
            EndRename(textBox);
            var selectedItem = renamingItem;
            string currentName = selectedItem.FileName;
            string newName = textBox.Text;

            if (newName == null)
                return;

            await App.OccupiedInstance.instanceInteraction.RenameFileItem(selectedItem, currentName, newName);
        }

        private void EndRename(TextBox textBox)
        {
            StackPanel parentPanel = textBox.Parent as StackPanel;
            TextBlock textBlock = parentPanel.Children[0] as TextBlock;
            textBox.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Visible;
            textBox.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;
            isRenamingItem = false;
        }

        private void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (!isRenamingItem)
                {
                    App.OccupiedInstance.instanceInteraction.List_ItemClick(null, null);
                    e.Handled = true;
                }
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                AssociatedInteractions.ShowPropertiesButton_Click(null, null);
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (App.OccupiedInstance != null)
            {
                if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    base.Page_CharacterReceived(sender, args);
                    FileList.Focus(FocusState.Keyboard);
                }
            }
        }
    }
}
