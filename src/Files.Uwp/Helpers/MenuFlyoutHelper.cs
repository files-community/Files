using CommunityToolkit.Mvvm.Input;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.Helpers
{
    public class MenuFlyoutHelper : DependencyObject
    {
        #region View Models

        public interface IMenuFlyoutItem
        {
            public MenuFlyoutItemBase Build();
        }

        public class MenuFlyoutSeparatorViewModel : IMenuFlyoutItem
        {
            public MenuFlyoutItemBase Build() => new MenuFlyoutSeparator();
        }

        public abstract class MenuFlyoutItemBaseViewModel : IMenuFlyoutItem
        {
            public string Text { get; }

            public bool IsEnabled { get; set; } = true;

            public MenuFlyoutItemBaseViewModel(string text) => Text = text;

            public abstract MenuFlyoutItemBase Build();
        }

        public class MenuFlyoutItemViewModel : MenuFlyoutItemBaseViewModel
        {
            public string Path { get; }

            public RelayCommand<string> OnSelect { get; }

            public MenuFlyoutItemViewModel(string text, string path, RelayCommand<string> onSelect) : base(text)
            {
                Path = path;
                OnSelect = onSelect;
            }

            public override MenuFlyoutItemBase Build()
            {
                var mfi = new MenuFlyoutItem
                {
                    Text = this.Text,
                    Command = this.OnSelect,
                    CommandParameter = this.Path,
                    IsEnabled = this.IsEnabled,
                };
                if (!string.IsNullOrEmpty(this.Path))
                {
                    ToolTipService.SetToolTip(mfi, this.Path);
                }
                return mfi;
            }
        }

        public class MenuFlyoutSubItemViewModel : MenuFlyoutItemBaseViewModel
        {
            public IList<IMenuFlyoutItem> Items { get; } = new List<IMenuFlyoutItem>();

            public MenuFlyoutSubItemViewModel(string text) : base(text)
            {
            }

            public override MenuFlyoutItemBase Build()
            {
                var mfsi = new MenuFlyoutSubItem
                {
                    Text = this.Text,
                    IsEnabled = this.IsEnabled && this.Items.Count > 0,
                };
                this.Items.ForEach(item => mfsi.Items.Add(item.Build()));
                return mfsi;
            }
        }

        public class MenuFlyoutCustomItemViewModel : IMenuFlyoutItem
        {
            public Func<MenuFlyoutItemBase> Factory { get; }

            public MenuFlyoutCustomItemViewModel(Func<MenuFlyoutItemBase> factory)
                => Factory = factory;

            public MenuFlyoutItemBase Build() => Factory();
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