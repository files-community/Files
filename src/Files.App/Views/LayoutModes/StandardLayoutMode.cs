using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.Interacts;
using Files.App.UserControls.Selection;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.App
{
	public abstract class StandardViewBase : BaseLayout
	{
		protected abstract ListViewBase ListViewBase
		{
			get;
		}

		protected override ItemsControl ItemsControl => ListViewBase;

		protected abstract SemanticZoom RootZoom
		{
			get;
		}

		public StandardViewBase() : base()
		{

		}

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
		}

		protected override void HookEvents()
		{
			UnhookEvents();
			ItemManipulationModel.FocusFileListInvoked += ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked += ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked += ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked += ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked += ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked += ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked += ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked += ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked += ItemManipulationModel_ScrollIntoViewInvoked;
		}

		protected override void UnhookEvents()
		{
			if (ItemManipulationModel is null)
				return;

			ItemManipulationModel.FocusFileListInvoked -= ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked -= ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked -= ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked -= ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked -= ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked -= ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked -= ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked -= ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked -= ItemManipulationModel_ScrollIntoViewInvoked;
		}

		protected virtual void ItemManipulationModel_FocusFileListInvoked(object? sender, EventArgs e)
		{
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
			var isFileListFocused = DependencyObjectHelpers.FindParent<ListViewBase>(focusedElement) == ItemsControl;
			if (!isFileListFocused)
				ListViewBase.Focus(FocusState.Programmatic);
		}

		protected virtual void ItemManipulationModel_SelectAllItemsInvoked(object? sender, EventArgs e)
		{
			ListViewBase.SelectAll();
		}

		protected virtual void ItemManipulationModel_ClearSelectionInvoked(object? sender, EventArgs e)
		{
			ListViewBase.SelectedItems.Clear();
		}

		protected virtual void ItemManipulationModel_InvertSelectionInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Count < GetAllItems().Count() / 2)
			{
				var oldSelectedItems = SelectedItems.ToList();
				ItemManipulationModel.SelectAllItems();
				ItemManipulationModel.RemoveSelectedItems(oldSelectedItems);
				return;
			}

			List<ListedItem> newSelectedItems = GetAllItems()
				.Cast<ListedItem>()
				.Except(SelectedItems)
				.ToList();

			ItemManipulationModel.SetSelectedItems(newSelectedItems);
		}

		protected virtual void ItemManipulationModel_StartRenameItemInvoked(object? sender, EventArgs e)
		{
			StartRenameItem();
		}

		protected abstract void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e);

		protected abstract void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e);

		protected abstract void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e);

		protected abstract void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e);

		protected virtual void ZoomIn(object? sender, GroupOption option)
		{
			if (option == GroupOption.None)
				RootZoom.IsZoomedInViewActive = true;
		}

		protected virtual async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItems = ListViewBase.SelectedItems.Cast<ListedItem>().Where(x => x is not null).ToList();
			if (SelectedItems.Count == 1 && App.AppModel.IsQuickLookAvailable)
				await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance, true);
		}

		protected virtual void SelectionRectangle_SelectionEnded(object? sender, EventArgs e)
		{
			ListViewBase.Focus(FocusState.Programmatic);
		}

		public override void Dispose()
		{
			base.Dispose();
			UnhookEvents();
			CommandsViewModel?.Dispose();
		}
	}
}
