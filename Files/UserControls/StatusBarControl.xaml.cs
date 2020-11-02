using Files.Interacts;
using Files.View_Models;
using System;
using Windows.UI.Xaml.Controls;


namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl
    {
        private Interaction InteractionOperations;
        public SettingsViewModel AppSettings => App.AppSettings;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; set; } = null;
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; set; } = null;

        public StatusBarControl()
        {
            this.InitializeComponent();
            InteractionOperations = DataContext as Interaction;
        }

        private void SelectAllMFI_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => InteractionOperations.SelectAllItems();

        private void InvertSelectionMFI_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => InteractionOperations.InvertAllItems();

        private void ClearSelectionMFI_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => InteractionOperations.ClearAllItems();
    }
}