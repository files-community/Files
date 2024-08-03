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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Files.App.Controls
{
	public partial class Toolbar : Control
	{

		private double _availableSize;	  						// A reference to the current available size for ToolbarItems

		private ItemsRepeater?				_itemsRepeater;
		private ToolbarItemList?			_toolbarItemsList;
		private ToolbarItemOverflowList?    _toolbarItemsOverflowList;


		private double				_smallMinWidth    = 24;     // I have set default values, but we pull from resources
		private double				_mediumMinWidth   = 32;     // if they are available.
		private double				_largeMinWidth    = 32;
		 
		private double				_smallMinHeight   = 24;
		private double				_mediumMinHeight  = 24;
		private double				_largeMinHeight   = 32;



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

		private ToolbarItemList GetToolbarItemsList()
		{
			return _toolbarItemsList;
		}

		private ToolbarItemOverflowList GetToolbarItemsOverflowList()
		{
			return _toolbarItemsOverflowList;
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

		private void SetToolbarItemsList(ToolbarItemList toolbarItemsList)
		{
			_toolbarItemsList = toolbarItemsList;
		}

		private void SetToolbarItemsList(ToolbarItemOverflowList toolbarItemsOverflowList)
		{
			_toolbarItemsOverflowList = toolbarItemsOverflowList;
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

			foreach ( ToolbarItem item in newItems )
			{
				if ( item != null )
				{
					if ( item.OverflowBehavior == OverflowBehaviors.Always )
					{
						AddItemToOverflowList( item );
					}
					else 
					{						
						/// For now we move it into the main list
						/// Eventually we need to check if there is room
						/// 
						AddItemToItemList( item );


						/// If there is no room for more items we must
						/// move them to the Overflow list, unless...
						/// 
						if ( item.OverflowBehavior == OverflowBehaviors.Never )
						{ 
							/// If the overflow behaviour is set to never, items
							/// without room will just be hidden.  This condition
							/// must be checked as the toolbar size changes.
						}
					}
				}
			}
		}



		/// <summary>
		/// EVENTUALLY REMOVE
		/// </summary>
		private void UpdateItemTemplate(DataTemplate newDataTemplate)
		{
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

			/// We need to check the Item Widths and Heights
			/// (we know the sizes for buttons, but content will need
			/// to be measured).  We also need to include the layout
			/// spacing values, to determine how many of our items can
			/// fit in the availableSize.
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

		private void PopulateItemsSourceForItemsRepeater()
		{
			/// We then need to measure each item from the ToolbarItemList
			/// we check the availableSize to see if there is room
			/// If there is room, we place it into the ItemsSource and
			/// remove that value from the availableSize + Spacing value
			/// 

			ItemsRepeater itemsRepeater = GetItemsRepeater();

			foreach ( ToolbarItem item in GetToolbarItemsList() )
			{
				/// We can get the AvailableSize
				/// 
				double availableSize = GetAvailableSize();				

				ObservableCollection<object> itemsSource = new ObservableCollection<object>();

				/// Then we create the ToolbarButton, ToolbarSeparator, ToolbarToggleButton etc
				/// for each item
				/// 
				if ( item.ItemType == ToolbarItemTypes.Button )
				{
					//Add a ToolbarButton to the ItemsSource for the ItemsRepeaterPartName
					itemsSource.Add( new ToolbarButton
					{ 
						Label = item.Label , 
						ThemedIcon = item.ThemedIcon , Command = item.Command , 
						CommandParameter = item.CommandParameter , 
						IconSize = item.IconSize,
					} );
				};

				if ( item.ItemType == ToolbarItemTypes.ToggleButton )
				{
					//Add a ToolbarToggleButton to the ItemsSource for the ItemsRepeaterPartName
					itemsSource.Add( new ToolbarToggleButton
					{
						Label = item.Label ,
						ThemedIcon = item.ThemedIcon ,
						Command = item.Command ,
						CommandParameter = item.CommandParameter ,
						IconSize = item.IconSize ,
					} );
				};

				if ( item.ItemType == ToolbarItemTypes.Separator )
				{
					//Add a ToolbarSeparator to the ItemsSource for the ItemsRepeaterPartName
					//itemsSource.Add( new ToolbarSeparator );
				};
				/// etc

				//SetAvailableSize( availableSize - item.Width );

				/// Once we have gone over the items and there is no more
				/// items to add, then we set the ItemsRepeater's ItemsSource
				///
				itemsRepeater.ItemsSource = itemsSource;
			}

				/// We do this for each item until there is no more space
				/// available, then we check its OverflowBehavior and move
				/// it if neccessary.

			}



		private void PopulateItemsSourceForOverflowMenu()
		{
			/// After the sorting for the ItemsRepeater ItemsSource
			/// whatever was put into the ToolbarItemOverflowList
			/// is what we need to add to the Overflow Menu
			/// 

			foreach ( ToolbarItem item in GetToolbarItemsOverflowList() )
			{
				if ( item != null )
				{
					if ( item.ItemType != ToolbarItemTypes.Separator )
					{ 
						MenuFlyoutSeparator menuFlyoutSeparator = new MenuFlyoutSeparator();
						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSeparator );
					}

					if ( item.ItemType != ToolbarItemTypes.FlyoutButton )
					{
						MenuFlyoutSubItem menuFlyoutSubItem = new MenuFlyoutSubItem();
						menuFlyoutSubItem.Text = item.Label;
						//menuFlyoutSubItem.ThemedIcon = item.ThemedIcon ;
						//menuFlyoutSubItem.IconSize = item.IconSize ;
						//menuFlyoutSubItem.Items.Add ;

						/// We will need to make child menu items for the 
						/// ToolbarItem's flyout items.
						/// 
					
						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSubItem );
					}

					if ( item.ItemType != ToolbarItemTypes.Button )
					{
						MenuFlyoutItem menuFlyoutItem= new MenuFlyoutItem();
						//MenuFlyoutItemEx menuFlyoutItemEx = new MenuFlyoutItemEx();
						menuFlyoutItem.Text = item.Label;
						menuFlyoutItem.Command = item.Command;
						menuFlyoutItem.CommandParameter = item.CommandParameter;
						menuFlyoutItem.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
						//menuFlyoutItemEx.ThemedIcon = item.ThemedIcon ;
						//menuFlyoutItemEx.IconSize = item.IconSize ;

						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSeparator );
					}

					if ( item.ItemType != ToolbarItemTypes.ToggleButton )
					{
						ToggleMenuFlyoutItem menuToggleItem = new ToggleMenuFlyoutItem();
						//ToggleMenuFlyoutItemEx menuToggleItemEx = new ToggleMenuFlyoutItemEx();
						menuToggleItem.Text = item.Label;
						menuToggleItem.Command = item.Command;
						menuToggleItem.CommandParameter = item.CommandParameter;
						menuToggleItem.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
						//mmenuToggleItemEx.ThemedIcon = item.ThemedIcon ;
						//menuToggleItemEx.IconSize = item.IconSize ;

						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSeparator );
					}

				}
			}
		}

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
