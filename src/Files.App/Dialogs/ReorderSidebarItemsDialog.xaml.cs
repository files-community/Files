using CommunityToolkit.WinUI.UI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

		private async void MoveItem(object sender, PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;
			var icon = sender as FontIcon;
			if (!properties.IsLeftButtonPressed)
				return;

			var navItem = icon?.FindAscendant<Grid>();
			if (navItem is not null)
				await navItem.StartDragAsync(e.GetCurrentPoint(navItem));
		}

		private void ListViewItem_DragStarting(object sender, DragStartingEventArgs e)
		{
			if (sender is not Grid nav || nav.DataContext is not LocationItem)
				return;

			// Adding the original Location item dragged to the DragEvents data view
			e.Data.Properties.Add("sourceLocationItem", nav);
			e.AllowedOperations = DataPackageOperation.Move;
		}

		
		private void ListViewItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as Grid)?.DataContext is not LocationItem locationItem)
				return;
			var deferral = e.GetDeferral();
			
			if ((e.DataView.Properties["sourceLocationItem"] as Grid)?.DataContext is LocationItem sourceLocationItem)
			{
				DragOver_SetCaptions(sourceLocationItem, locationItem, e);
			}

			deferral.Complete();
		}

		private void DragOver_SetCaptions(LocationItem senderLocationItem, LocationItem sourceLocationItem, DragEventArgs e)
		{
			// If the location item is the same as the original dragged item
			if (sourceLocationItem.CompareTo(senderLocationItem) == 0)
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
			if (sender is not Grid navView || navView.DataContext is not LocationItem locationItem)
				return;

			if ((e.DataView.Properties["sourceLocationItem"] as Grid)?.DataContext is LocationItem sourceLocationItem)
				ViewModel.SidebarFavoriteItems.Move(ViewModel.SidebarFavoriteItems.IndexOf(sourceLocationItem), ViewModel.SidebarFavoriteItems.IndexOf(locationItem));
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
	}
}