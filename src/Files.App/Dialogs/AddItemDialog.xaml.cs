// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	/// <summary>
	/// Represents an <see cref="ContentDialog"/> UI for storage item addition.
	/// </summary>
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
		{
			return (DialogResult)await base.ShowAsync();
		}

		private async void AddItemDialog_Loaded(object sender, RoutedEventArgs e)
		{
			var itemTypes = await _addItemService.GetNewEntriesAsync();
			await ViewModel.AddItemsToList(itemTypes);

			// Focus on the ListView so that the users can use keyboard navigation
			AddItemsListView.Focus(FocusState.Programmatic);
		}

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			ViewModel.ResultType = ((AddItemDialogListItemViewModel)e.ClickedItem)?.ItemResult
				?? throw new ArgumentNullException("ItemResult", $"{nameof(AddItemDialog)}.{nameof(ListView_ItemClick)}.e.ClickedItem was null.");

			Hide();
		}
	}
}
