using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class AddItemDialog : ContentDialog, IDialog<AddItemDialogViewModel>
	{
		private readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();

		public AddItemDialogViewModel ViewModel { get; set; }

		public AddItemDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			ViewModel.ResultType = (e.ClickedItem as AddItemDialogListItemViewModel).ItemResult;

			Hide();
		}

		private async void AddItemDialog_Loaded(object sender, RoutedEventArgs e)
		{
			var itemTypes = await addItemService.GetNewEntriesAsync();
			await ViewModel.AddItemsToList(itemTypes);

			// Focus on the list view so users can use keyboard navigation
			AddItemsListView.Focus(FocusState.Programmatic);
		}
	}
}
