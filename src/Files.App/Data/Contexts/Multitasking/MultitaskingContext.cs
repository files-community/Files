﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.UserControls.TabView;
using Files.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Files.App.Data.Contexts
{
	internal class MultitaskingContext : ObservableObject, IMultitaskingContext
	{
		private bool isPopupOpen = false;

		private ITabView? control;
		public ITabView? Control => control;

		private ushort tabCount = 0;
		public ushort TabCount => tabCount;

		public TabViewItem CurrentTabItem => MainPageViewModel.AppInstances[currentTabIndex];
		public TabViewItem SelectedTabItem => MainPageViewModel.AppInstances[selectedTabIndex];

		private ushort currentTabIndex = 0;
		public ushort CurrentTabIndex => currentTabIndex;

		private ushort selectedTabIndex = 0;
		public ushort SelectedTabIndex => selectedTabIndex;

		public MultitaskingContext()
		{
			MainPageViewModel.AppInstances.CollectionChanged += AppInstances_CollectionChanged;
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
			BaseTabView.OnLoaded += BaseMultitaskingControl_OnLoaded;
			TabView.SelectedTabItemChanged += HorizontalMultitaskingControl_SelectedTabItemChanged;
			FocusManager.GotFocus += FocusManager_GotFocus;
			FocusManager.LosingFocus += FocusManager_LosingFocus;
		}

		private void AppInstances_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateTabCount();
		}
		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			UpdateCurrentTabIndex();
		}
		private void BaseMultitaskingControl_OnLoaded(object? sender, ITabView control)
		{
			SetProperty(ref this.control, control, nameof(Control));
			UpdateTabCount();
			UpdateCurrentTabIndex();
		}
		private void HorizontalMultitaskingControl_SelectedTabItemChanged(object? sender, TabViewItem? e)
		{
			isPopupOpen = e is not null;
			int newSelectedIndex = e is null ? currentTabIndex : MainPageViewModel.AppInstances.IndexOf(e);
			UpdateSelectedTabIndex(newSelectedIndex);
		}
		private void FocusManager_GotFocus(object? sender, FocusManagerGotFocusEventArgs e)
		{
			if (isPopupOpen)
				return;

			if (e.NewFocusedElement is FrameworkElement element && element.DataContext is TabViewItem tabItem)
			{
				int newSelectedIndex = MainPageViewModel.AppInstances.IndexOf(tabItem);
				UpdateSelectedTabIndex(newSelectedIndex);
			}
		}
		private void FocusManager_LosingFocus(object? sender, LosingFocusEventArgs e)
		{
			if (isPopupOpen)
				return;

			if (SetProperty(ref selectedTabIndex, currentTabIndex, nameof(SelectedTabIndex)))
			{
				OnPropertyChanged(nameof(selectedTabIndex));
			}
		}

		private void UpdateTabCount()
		{
			SetProperty(ref tabCount, (ushort)MainPageViewModel.AppInstances.Count, nameof(TabCount));
		}
		private void UpdateCurrentTabIndex()
		{
			if (SetProperty(ref currentTabIndex, (ushort)App.AppModel.TabStripSelectedIndex, nameof(CurrentTabIndex)))
			{
				OnPropertyChanged(nameof(CurrentTabItem));
			}
		}
		private void UpdateSelectedTabIndex(int index)
		{
			if (SetProperty(ref selectedTabIndex, (ushort)index, nameof(SelectedTabIndex)))
			{
				OnPropertyChanged(nameof(SelectedTabItem));
			}
		}
	}
}
