// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Files.App.Controls
{
	public partial class Toolbar : Control
	{
		public Toolbar()
		{
			this.DefaultStyleKey = typeof( Toolbar );
		}



		#region Update Properties

		private void UpdateItemsProperty(object newObject)
		{ 
			///
			/// Reads in the ToolbarItem in the Toolbar.Items list
			/// when iterating through them, we ignore any that do not
			/// match the correct object of ToolbarItem
			/// 
			/// Then we read the porperties of each item and assign
			/// each item to the correct lists which we will use to 
			/// manage the Buttons and the Menu items
			///
		}



		private void OnToolbarSizePropertyChanged(ToolbarSizes newToolbarSize)
		{ 
			///
			/// We use this property value to control the size of the
			/// Buttons we display in the Toolbar.
			/// 
			/// This does not affect items in the overflow menu.
			///
		}

		#endregion



		#region Dealing with Overflow and StackPanel Part sizing
		#endregion



		#region Add ToolbarItem to Lists

		/// <summary>
		/// Adds a ToolbarItem into the ToolbarItemOverflowList
		/// </summary>
		private void AddItemToOverflowList(ToolbarItem item)
		{
			switch ( item.ItemType )
			{
				case ToolbarItemTypes.Button:
					// Add MenuFlyoutItemEx
					break;

				case ToolbarItemTypes.FlyoutButton:
					// Add MenuFlyoutSubItemEx
					break;

				case ToolbarItemTypes.RadioButton:
					// Add RadioMenuFlyoutItemEx
					break;

				case ToolbarItemTypes.SplitButton:
					// Add MenuFlyoutSubItemEx + MenuFlyoutItemEx
					break;

				case ToolbarItemTypes.ToggleButton:
					// Add MenuFlyoutToggleItemEx
					break;
			}
		}



		/// <summary>
		/// Adds a ToolbarItem into the ToolbarItemList
		/// </summary>
		private void AddItemToItemList(ToolbarItem item)
		{
			switch ( item.ItemType )
			{
				case ToolbarItemTypes.Button:
					// Add ToolbarButton
					break;

				case ToolbarItemTypes.FlyoutButton:
					// Add ToolbarFlyoutButton
					break;

				case ToolbarItemTypes.RadioButton:
					// Add ToolbarRadioButton
					break;

				case ToolbarItemTypes.SplitButton:
					// Add ToolbarSplitButton
					break;

				case ToolbarItemTypes.ToggleButton:
					// Add ToolbarToggleButton
					break;

				case ToolbarItemTypes.Content:
					// Add Content Presenter
					break;
			}
		}

		#endregion



		#region Remove ToolbarItem from Lists

		/// <summary>
		/// Removes a ToolbarItem from the ToolbarItemOverflowList
		/// </summary>
		private void RemoveItemFromOverflowList(ToolbarItem item)
		{

		}



		/// <summary>
		/// Removes a ToolbarItem from the ToolbarItemList
		/// </summary>
		private void RemoveItemFromList(ToolbarItem item)
		{

		}

		#endregion

	}
}
