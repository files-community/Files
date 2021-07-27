using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Il modello di elemento Controllo utente è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class MenuFlyoutItemFileTag : MenuFlyoutItem
    {
        public FileTag SelectedItem
        {
            get { return (FileTag)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(FileTag), typeof(MenuFlyoutItemFileTag), new PropertyMetadata(null));

        public IList<FileTag> ItemsSource
        {
            get { return (IList<FileTag>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList<FileTag>), typeof(MenuFlyoutItemFileTag), new PropertyMetadata(null));

        public MenuFlyoutItemFileTag()
        {
            this.InitializeComponent();
            this.ItemsSource = new List<FileTag>();
        }

        private async void TagList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var listView = sender as ListView;
            if (e.ClickedItem == listView.SelectedItem)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => listView.SelectedItem = null);
            }
        }
    }
}
