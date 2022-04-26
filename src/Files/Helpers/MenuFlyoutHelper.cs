using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    public class MenuFlyoutHelper : DependencyObject
    {
        #region View Models

        public interface IMenuFlyoutItem { }

        public class MenuFlyoutSeparatorViewModel : IMenuFlyoutItem { }

        public abstract class MenuFlyoutItemBaseViewModel : IMenuFlyoutItem
        {
            public string Text { get; }

            public bool IsEnabled { get; set; } = true;

            internal MenuFlyoutItemBaseViewModel(string text) => Text = text;
        }

        public class MenuFlyoutItemViewModel : MenuFlyoutItemBaseViewModel
        {
            public string Path { get; }

            public RelayCommand<string> OnSelect { get; }

            internal MenuFlyoutItemViewModel(string text, string path, RelayCommand<string> onSelect) : base(text)
            {
                Path = path;
                OnSelect = onSelect;
            }
        }

        public class MenuFlyoutSubItemViewModel : MenuFlyoutItemBaseViewModel
        {
            public IList<IMenuFlyoutItem> Items { get; } = new List<IMenuFlyoutItem>();

            internal MenuFlyoutSubItemViewModel(string text) : base(text)
            {
            }
        }

        #endregion View Models

        #region ItemsSource

        public static IEnumerable<IMenuFlyoutItem> GetItemsSource(DependencyObject obj) => obj.GetValue(ItemsSourceProperty) as IEnumerable<IMenuFlyoutItem>;

        public static void SetItemsSource(DependencyObject obj, IEnumerable<IMenuFlyoutItem> value) => obj.SetValue(ItemsSourceProperty, value);

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable<IMenuFlyoutItem>), typeof(MenuFlyoutHelper), new PropertyMetadata(null, ItemsSourceChanged));

        private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => SetupItems(d as MenuFlyout);

        #endregion ItemsSource

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
                AddItems(menu.Items, itemSource);
            });
        }

        private static void AddItems(IList<MenuFlyoutItemBase> menu, IEnumerable<IMenuFlyoutItem> items)
        {
            foreach (var item in items)
            {
                if (item is MenuFlyoutSeparatorViewModel)
                {
                    menu.Add(new MenuFlyoutSeparator());
                }
                else if (item is MenuFlyoutItemViewModel vm)
                {
                    var mfi = new MenuFlyoutItem
                    {
                        Text = vm.Text,
                        Command = vm.OnSelect,
                        CommandParameter = vm.Path,
                        IsEnabled = vm.IsEnabled,
                    };
                    if (!string.IsNullOrEmpty(vm.Path))
                    {
                        ToolTipService.SetToolTip(mfi, vm.Path);
                    }
                    menu.Add(mfi);
                }
                else if (item is MenuFlyoutSubItemViewModel svm)
                {
                    var mfsi = new MenuFlyoutSubItem
                    {
                        Text = svm.Text,
                        IsEnabled = svm.IsEnabled && svm.Items.Count > 0,
                    };
                    AddItems(mfsi.Items, svm.Items);
                    menu.Add(mfsi);
                }
            }
        }
    }
}