// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.Helpers
{
	public class MenuFlyoutHelper : DependencyObject
	{
		#region View Models

		public interface IMenuFlyoutItemViewModel { }

		public class MenuFlyoutSeparatorViewModel : IMenuFlyoutItemViewModel { }

		public class MenuFlyoutItemViewModel : IMenuFlyoutItemViewModel
		{
			public string Text { get; init; }

			public ICommand Command { get; init; }

			public object CommandParameter { get; init; }

			public string Tooltip { get; init; }

			public bool IsEnabled { get; set; } = true;

			public MenuFlyoutItemViewModel(string text)
				=> Text = text;
		}

		public class MenuFlyoutSubItemViewModel : IMenuFlyoutItemViewModel
		{
			public string Text { get; }

			public bool IsEnabled { get; set; } = true;

			public IList<IMenuFlyoutItemViewModel> Items { get; } = new List<IMenuFlyoutItemViewModel>();

			public MenuFlyoutSubItemViewModel(string text)
				=> Text = text;
		}

		public class MenuFlyoutFactoryItemViewModel : IMenuFlyoutItemViewModel
		{
			public Func<MenuFlyoutItemBase> Build { get; }

			public MenuFlyoutFactoryItemViewModel(Func<MenuFlyoutItemBase> factoryFunc)
				=> Build = factoryFunc;
		}

		#endregion View Models

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

		private static async Task SetupItems(MenuFlyout menu)
		{
			if (menu is null || Windows.ApplicationModel.DesignMode.DesignModeEnabled)
			{
				return;
			}
			var itemSource = GetItemsSource(menu);
			if (itemSource is null)
			{
				return;
			}

			await menu.DispatcherQueue.EnqueueAsync(() =>
			{
				menu.Items.Clear();
				AddItems(menu.Items, itemSource);
			});
		}

		private static void AddItems(IList<MenuFlyoutItemBase> menu, IEnumerable<IMenuFlyoutItemViewModel> items)
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
						Command = vm.Command,
						CommandParameter = vm.CommandParameter,
						IsEnabled = vm.IsEnabled,
					};
					if (!string.IsNullOrEmpty(vm.Tooltip))
					{
						ToolTipService.SetToolTip(mfi, vm.Tooltip);
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
				else if (item is MenuFlyoutFactoryItemViewModel fvm)
				{
					menu.Add(fvm.Build());
				}
			}
		}
	}
}