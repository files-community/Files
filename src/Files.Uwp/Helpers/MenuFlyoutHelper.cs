using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.Helpers
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

        public class MenuFlyoutCustomItemViewModel : IMenuFlyoutItem
        {
            public string TemplateKey { get; }

            public object DataContext { get; }

            public MenuFlyoutCustomItemViewModel(object dataContext, string templateKey)
                => (DataContext, TemplateKey) = (dataContext, templateKey);
        }

        #endregion View Models

        #region ItemsSource

        public static IEnumerable<IMenuFlyoutItem> GetItemsSource(DependencyObject obj) => obj.GetValue(ItemsSourceProperty) as IEnumerable<IMenuFlyoutItem>;

        public static void SetItemsSource(DependencyObject obj, IEnumerable<IMenuFlyoutItem> value) => obj.SetValue(ItemsSourceProperty, value);

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable<IMenuFlyoutItem>), typeof(MenuFlyoutHelper), new PropertyMetadata(null, ItemsSourceChanged));

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

        #region ItemTemplates

        public static IDictionary<string, DataTemplate> GetItemTemplates(DependencyObject obj)
        {
            if (obj.GetValue(ItemTemplatesProperty) is IDictionary<string, DataTemplate> value)
            {
                return value;
            }

            var defValue = new Dictionary<string, DataTemplate>();
            obj.SetValue(ItemTemplatesProperty, defValue);

            return defValue;
        }

        public static void SetItemTemplates(DependencyObject obj, IDictionary<string, DataTemplate> value)
        {
            obj.SetValue(ItemTemplatesProperty, value);
        }

        public static readonly DependencyProperty ItemTemplatesProperty =
            DependencyProperty.RegisterAttached("ItemTemplates", typeof(IDictionary<string, DataTemplate>), typeof(MenuFlyoutHelper), new PropertyMetadata(null, ItemTemplatesChanged));

        private static void ItemTemplatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => SetupItems(d as MenuFlyout);

        #endregion

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

            var itemTemplates = GetItemTemplates(menu);
            if (itemTemplates == null && itemSource.Any(x => x is MenuFlyoutCustomItemViewModel))
            {
                return;
            }

            await menu.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                menu.Items.Clear();
                AddItems(menu.Items, itemSource, itemTemplates);
            });
        }

        private static void AddItems(IList<MenuFlyoutItemBase> menu, IEnumerable<IMenuFlyoutItem> items, IDictionary<string, DataTemplate> itemTemplates)
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
                    AddItems(mfsi.Items, svm.Items, itemTemplates);
                    menu.Add(mfsi);
                }
                else if (item is MenuFlyoutCustomItemViewModel cvm)
                {
                    // Throw error if key not found or not derived from MenuFlyoutItemBase
                    var mfci = (MenuFlyoutItemBase)itemTemplates[cvm.TemplateKey].LoadContent();
                    mfci.DataContext = cvm.DataContext;
                    menu.Add(mfci);
                }
            }
        }
    }
}