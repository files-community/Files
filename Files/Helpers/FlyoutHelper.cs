using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    public class FlyoutHelper : DependencyObject
    {
        #region ItemsSource

        public static IEnumerable GetItemsSource(DependencyObject obj) => obj.GetValue(ItemsSourceProperty) as IEnumerable;

        public static void SetItemsSource(DependencyObject obj, IEnumerable value) => obj.SetValue(ItemsSourceProperty, value);

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable), typeof(FlyoutHelper), new PropertyMetadata(null, ItemsSourceChanged));

        private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => SetupItems(d as Flyout);

        #endregion

        #region ItemTemplate

        public static DataTemplate GetItemTemplate(DependencyObject obj) => (DataTemplate)obj.GetValue(ItemTemplateProperty);

        public static void SetItemTemplate(DependencyObject obj, DataTemplate value) => obj.SetValue(ItemTemplateProperty, value);

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.RegisterAttached("ItemTemplate", typeof(DataTemplate), typeof(FlyoutHelper), new PropertyMetadata(null, ItemsTemplateChanged));

        private static void ItemsTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => SetupItems(d as Flyout);

        #endregion

        private static async void SetupItems(Flyout flyout)
        {
            if (flyout == null || Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                return;
            }
            var itemSource = GetItemsSource(flyout);
            if (itemSource == null)
            {
                return;
            }
            var itemTemplate = GetItemTemplate(flyout);
            if (itemTemplate == null)
            {
                return;
            }

            await flyout.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => flyout.Content = new ItemsControl
            {
                ItemsSource = itemSource,
                ItemTemplate = itemTemplate,
            });
        }
    }
}
