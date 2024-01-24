// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.Data.Items;
using Files.App.Extensions;
using Files.App.ViewModels.Dialogs;
using Files.Core.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Dialogs
{
	public sealed partial class ReorderSidebarItemsDialog : ContentDialog, IDialog<ReorderSidebarItemsDialogViewModel>
	{
		public ReorderSidebarItemsDialogViewModel ViewModel
		{
			get => (ReorderSidebarItemsDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ReorderSidebarItemsDialog()
		{
			InitializeComponent();
		}

		private async void MoveItemAsync(object sender, PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;
			if (!properties.IsLeftButtonPressed)
				return;

			var icon = sender as FontIcon;

			var navItem = icon?.FindAscendant<Grid>();
			if (navItem is not null)
				await navItem.StartDragAsync(e.GetCurrentPoint(navItem));
		}

		private void ListViewItem_DragStarting(object sender, DragStartingEventArgs e)
		{
			if (sender is not Grid nav || nav.DataContext is not INavigationControlItem)
				return;

			// Adding the original Location item dragged to the DragEvents data view
			e.Data.Properties.Add("sourceItem", nav);
			e.AllowedOperations = DataPackageOperation.Move;
		}

		private void ListViewItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as Grid)?.DataContext is not INavigationControlItem item)
				return;
			var deferral = e.GetDeferral();
			
			if ((e.DataView.Properties["sourceItem"] as Grid)?.DataContext is INavigationControlItem sourceItem)
			{
				DragOver_SetCaptions(sourceItem, item, e);
			}

			deferral.Complete();
		}

		private void DragOver_SetCaptions(INavigationControlItem senderItem, INavigationControlItem sourceItem, DragEventArgs e)
		{
			// If the location item is the same as the original dragged item
			if (sourceItem.CompareTo(senderItem) == 0)
			{
				e.AcceptedOperation = DataPackageOperation.None;
				e.DragUIOverride.IsCaptionVisible = false;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = "MoveItemsDialogPrimaryButtonText".GetLocalizedResource();
				e.AcceptedOperation = DataPackageOperation.Move;
			}
		}

		private void ListViewItem_Drop(object sender, DragEventArgs e)
		{
			if (sender is not Grid navView || navView.DataContext is not INavigationControlItem item)
				return;

			if ((e.DataView.Properties["sourceItem"] as Grid)?.DataContext is INavigationControlItem sourceItem)
				ViewModel.SidebarFavoriteItems.Move(ViewModel.SidebarFavoriteItems.IndexOf(sourceItem), ViewModel.SidebarFavoriteItems.IndexOf(item));
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}
	}
}
