using Files.App.DataModels.NavigationControlItems;
using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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

		private void ReorderUp_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button clickButton || clickButton.DataContext is not LocationItem item)
				return;

			int index = ViewModel.SidebarFavoriteItems.IndexOf(item) - 1 >= 0 
				? ViewModel.SidebarFavoriteItems.IndexOf(item) - 1 : ViewModel.SidebarFavoriteItems.IndexOf(item);
			ViewModel.SidebarFavoriteItems.Remove(item);
			ViewModel.SidebarFavoriteItems.Insert(index, item);
		}

		private void ReorderDown_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button clickButton || clickButton.DataContext is not LocationItem item)
				return;

			int index = ViewModel.SidebarFavoriteItems.IndexOf(item) + 1 < ViewModel.SidebarFavoriteItems.Count 
				? ViewModel.SidebarFavoriteItems.IndexOf(item) + 1 : ViewModel.SidebarFavoriteItems.IndexOf(item);
			ViewModel.SidebarFavoriteItems.Remove(item);
			ViewModel.SidebarFavoriteItems.Insert(index, item);
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
	}
}