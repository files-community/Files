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

		private double			_availableSize;	  	        // A reference to the current available size for ToolbarItems

		private ItemsRepeater?	_itemsRepeater;
		 
		private double			_smallMinWidth    = 24;     // I have set default values, but we pull from resources
		private double			_mediumMinWidth   = 32;     // if they are available.
		private double			_largeMinWidth    = 32;
		 
		private double			_smallMinHeight   = 24;
		private double			_mediumMinHeight  = 24;
		private double			_largeMinHeight   = 32;



		public Toolbar()
		{
			this.DefaultStyleKey = typeof( Toolbar );
		}



		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}



		#region Private Getters

		private double GetAvailableSize()
		{
			return _availableSize;
		}

		private ItemsRepeater GetItemsRepeater()
		{
			return _itemsRepeater;
		}

		private double GetSmallMinWidth()
		{
			return _smallMinWidth;
		}

		private double GeMediumMinWidth()
		{
			return _mediumMinWidth;
		}

		private double GetLargeMinWidth()
		{
			return _largeMinWidth;
		}

		private double GetSmallMinHeight()
		{
			return _smallMinHeight;
		}

		private double GetMediumMinHeight()
		{
			return _mediumMinHeight;
		}

		private double GetLargeMinHeight()
		{
			return _largeMinHeight;
		}

		#endregion



		#region Private Setters

		private void SetAvailableSize(double newValue)
		{
			_availableSize = newValue;
		}

		private void SetItemsRepeater(ItemsRepeater itemsRepeater)
		{
			_itemsRepeater = itemsRepeater;
		}

		private void SetSmallMinWidth(double newValue)
		{
			_smallMinWidth = newValue;
		}

		private void SetMediumMinWidth(double newValue)
		{
			_mediumMinWidth = newValue;
		}

		private void SetLargeMinWidth(double newValue)
		{
			_largeMinWidth = newValue;
		}

		private void SetSmallMinHeight(double newValue)
		{
			_smallMinHeight = newValue;
		}

		private void SetMediumMinHeight(double newValue)
		{
			_mediumMinHeight = newValue;
		}

		private void SetLargeMinHeight(double newValue)
		{
			_largeMinHeight = newValue;
		}

		#endregion



		#region Update Properties

		/// <summary>
		/// Updates the Toolbar's Items property
		/// </summary>
		/// <param name="newItems"></param>
		private void UpdateItems(IList<ToolbarItem> newItems)
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

			

			// clear both lists and re-add new items
		}



		/// <summary>
		/// Updates the Toolbar's Items property
		/// </summary>
		/// <param name="newObject"></param>
		private void UpdateItemTemplate(DataTemplate newDataTemplate)
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

			// clear both lists and re-add new items
		}



		private void UpdateToolbarSize( ToolbarSizes newToolbarSize )
		{
			switch ( newToolbarSize )
			{
				case ToolbarSizes.Small:
					{
						// for items in ToolbarItemList
						// Update button sizes to small
					}
					break;

				case ToolbarSizes.Medium:
					{
						// for items in ToolbarItemList
						// Update button sizes to medium
					}
					break;

				case ToolbarSizes.Large:
					{
						// for items in ToolbarItemList
						// Update button sizes to large
					}
					break;
			}
		}



		private void UpdateAvailableSize()
		{
			double newAvailableSize = 0;

			// Do some code to check or respond to size changes for
			// the Toolbar's Items space (ItemsRepeaterLayout?)

			SetAvailableSize( newAvailableSize );
		}



		/// <summary>
		/// Updates the MinWidth and MinHeight for Toolbar Buttons, from
		/// Resources, or uses initial values we set in code.
		/// </summary>
		private void UpdateMinSizesFromResources()
		{
			double smallMinWidth    = (double)Application.Current.Resources[SmallMinWidthResourceKey];
			double smallMinHeight   = (double)Application.Current.Resources[SmallMinHeightResourceKey];

			double mediumMinWidth   = (double)Application.Current.Resources[MediumMinWidthResourceKey];
			double mediumMinHeight  = (double)Application.Current.Resources[MediumMinHeightResourceKey];

			double largeMinWidth    = (double)Application.Current.Resources[LargeMinWidthResourceKey];
			double largeMinHeight   = (double)Application.Current.Resources[LargeMinHeightResourceKey];


			if ( !double.IsNaN( smallMinWidth )  || !double.IsNaN( smallMinHeight )  ||
				 !double.IsNaN( mediumMinWidth ) || !double.IsNaN( mediumMinHeight ) ||
				 !double.IsNaN( largeMinWidth )  || !double.IsNaN( largeMinHeight )    )
			{
				SetSmallMinWidth( smallMinWidth );
				SetSmallMinHeight( smallMinHeight );

				SetMediumMinWidth( mediumMinWidth );
				SetMediumMinHeight( mediumMinHeight );

				SetLargeMinWidth( largeMinWidth );
				SetLargeMinHeight( largeMinHeight );
			}
		}

		#endregion



		#region Property Changed Events

		/// <summary>
		/// Handles changes to the Items
		/// </summary>
		/// <param name="newItemsSource"></param>
		private void ItemsChanged(IList<ToolbarItem> newItems)
		{
			UpdateItems( newItems );
		}



		/// <summary>
		/// Handles changes to the ItemTemplate
		/// </summary>
		/// <param name="newDataTemplate"></param>
		private void ItemTemplateChanged(DataTemplate newDataTemplate)
		{
			UpdateItemTemplate( newDataTemplate );
		}



		/// <summary>
		/// Handles changes to the ToolbarSize property
		/// </summary>
		/// <param name="newToolbarSize"></param>
		private void ToolbarSizeChanged(ToolbarSizes newToolbarSize)
		{
			UpdateToolbarSize( newToolbarSize );
		}

		#endregion



		#region Dealing with Overflow and StackPanel Part sizing

		///
		/// We will need methods that handle when the availableSize
		/// is too small to display all the ToolbarItemList
		/// items, so we can remove from the list and add to
		/// the ToolbarItemOverflowList.
		/// 
		/// If the availableSize is increased, then we can move
		/// an item that is not set with OverflowBehavior.Always
		/// out of the ToolbarItemOverflowList, and back into
		/// the ToolbarItemList/
		///

		// UpdateAvailableSize();

		#endregion



		#region Add ToolbarItems to Lists

		/// <summary>
		/// Reads a ToolbarItem and adds equivalent Item
		/// into the ToolbarItemOverflowList
		/// </summary>
		/// <param name="item"></param>
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
		/// Reads a ToolbarItem and adds equivalent Item
		/// into the ToolbarItemList
		/// </summary>
		/// <param name="item"></param>
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
		/// <param name="item"></param>
		private void RemoveItemFromOverflowList(ToolbarItem item)
		{

		}



		/// <summary>
		/// Removes a ToolbarItem from the ToolbarItemList
		/// </summary>
		/// <param name="item"></param>
		private void RemoveItemFromList(ToolbarItem item)
		{

		}

		#endregion

	}
}
