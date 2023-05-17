// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class AddItemDialog : ContentDialog, IDialog<AddItemDialogViewModel>
	{
		private readonly IAddItemService _addItemService;

		public AddItemDialogViewModel ViewModel { get; set; }

		public AddItemDialog()
		{
			InitializeComponent();

			// Dependency Injection
			_addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();

		private async void AddItemDialog_Loaded(object sender, RoutedEventArgs e)
		{
			var itemTypes = await _addItemService.GetNewEntriesAsync();
			await ViewModel.AddItemsToList(itemTypes);

			// Focus on the list view so users can use keyboard navigation
			AddItemsListView.Focus(FocusState.Programmatic);
		}

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			ViewModel.ResultType = ((AddItemDialogListItemViewModel)e.ClickedItem)?.ItemResult ?? throw new ArgumentNullException();

			Hide();
		}
	}
}
