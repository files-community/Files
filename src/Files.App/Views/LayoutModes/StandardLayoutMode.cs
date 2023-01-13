using Files.App.Helpers.XamlHelpers;
using Files.App.Interacts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace Files.App.Views.LayoutModes
{
	public abstract class StandardLayoutMode : BaseLayout
	{
		protected override uint IconSize => throw new System.NotImplementedException();

		protected override bool CanGetItemFromElement(object element)
		{
			throw new System.NotImplementedException();
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

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
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

		protected void ItemManipulationModel_FocusFileListInvoked(object? sender, EventArgs e)
		{
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
			var isFileListFocused = DependencyObjectHelpers.FindParent<ListViewBase>(focusedElement) == ItemsControl;
			if (!isFileListFocused)
				ItemsControl.Focus(FocusState.Programmatic);
		}

		protected void ItemManipulationModel_SelectAllItemsInvoked(object? sender, EventArgs e)
		{
			ItemsControl.SelectAll();
		}
	}
}
