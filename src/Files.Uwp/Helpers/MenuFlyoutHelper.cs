using CommunityToolkit.Mvvm.Input;
using Files.Shared.Extensions;
using Files.Uwp.ViewModels;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.Helpers
{
    public class MenuFlyoutHelper : DependencyObject
    {
        #region ItemsSource

        public static IEnumerable<IMenuFlyoutItemViewModel> GetItemsSource(DependencyObject obj) => obj.GetValue(ItemsSourceProperty) as IEnumerable<IMenuFlyoutItemViewModel>;

        public static void SetItemsSource(DependencyObject obj, IEnumerable<IMenuFlyoutItemViewModel> value) => obj.SetValue(ItemsSourceProperty, value);

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable<IMenuFlyoutItemViewModel>), typeof(MenuFlyoutHelper), new PropertyMetadata(null, ItemsSourceChanged));

        private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => SetupItems(d as MenuFlyout);

        #endregion ItemsSource

        #region IsVisible

        public static bool GetIsVisible(DependencyObject d) => (bool)d.GetValue(IsVisibleProperty);

        public static void SetIsVisible(DependencyObject d, bool value) => d.SetValue(IsVisibleProperty, value);

        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(MenuFlyoutHelper), new PropertyMetadata(false, OnIsVisiblePropertyChanged));

        private static void OnIsVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MenuFlyout flyout)
            {
                return;
            }

            var boolValue = (bool)e.NewValue;

            // hide the MenuFlyout if it's bool is false.
            if (!boolValue)
                flyout.Hide();
        }

        #endregion IsVisible

        private static async void SetupItems(MenuFlyout menu)
        {
            if (menu == null || Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                return;
            }
            var itemSource = GetItemsSource(menu);
            if (itemSource == null)
            {
                return;
            }

            await menu.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                menu.Items.Clear();
                itemSource.ForEach(item => menu.Items.Add(item.Build()));
            });
        }
    }
}