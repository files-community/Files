// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a model class that is intended to be used with <see cref="ContextFlyoutItemHelper"/> and
	/// <see cref="Helpers.ContextFlyouts.ItemModelListToContextFlyoutHelper"/>.
	/// </summary>
	public class ContextMenuFlyoutItem
	{
		/// <summary>
		/// Gets or sets a value that indicates that the ContextMenuItem should be displayed.
		/// </summary>
		public bool ShowItem { get; set; } = true;

		/// <summary>
		/// Gets or sets the command to invoke when this button is pressed.
		/// </summary>
		public ICommand? Command { get; set; }

		/// <summary>
		/// Gets or sets the parameter to pass to the Command property.
		/// </summary>
		public object? CommandParameter { get; set; }

		/// <summary>
		/// Gets or sets the graphic content of the menu flyout item.
		/// </summary>
		public string? Glyph { get; set; }

		/// <summary>
		/// Gets or sets the font used to display graphic content of the menu flyout item.
		/// </summary>
		public string? GlyphFontFamilyName { get; set; }

		/// <summary>
		/// Gets or sets a string that overrides the default key combination string associated with a keyboard accelerator.
		/// </summary>
		public string? KeyboardAcceleratorTextOverride { get; set; }

		/// <summary>
		/// Gets or sets the text to display.
		/// </summary>
		public string? Text { get; set; }

		/// <summary>
		/// Gets or sets an arbitrary object value that can be used to store custom information about this object.
		/// </summary>
		public object? Tag { get; set; }

		/// <summary>
		/// Gets or sets the type of the menu flyout item.
		/// </summary>
		public ContextMenuFlyoutItemType ItemType { get; set; }

		/// <summary>
		/// Gets or sets an action that load the sub menu flyout
		/// </summary>
		public Func<Task>? LoadSubMenuAction { get; set; }

		/// <summary>
		/// Gets or sets a list that is the List of ContextMenuFlyoutItem.
		/// </summary>
		public List<ContextMenuFlyoutItem>? Items { get; set; }

		/// <summary>
		/// Gets or sets the bitmap icon content of the menu flyout item.
		/// </summary>
		public BitmapImage? BitmapIcon { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is displayed only when the shift key is held.
		/// </summary>
		public bool ShowOnShift { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is displayed only when one item is selected.
		/// </summary>
		public bool SingleItemOnly { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is displayed in the Recycle Bin page.
		/// </summary>
		public bool ShowInRecycleBin { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is displayed in Search page.
		/// </summary>
		public bool ShowInSearchPage { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is displayed in FTP page.
		/// </summary>
		public bool ShowInFtpPage { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is displayed in ZIP Archive page.
		/// </summary>
		public bool ShowInZipPage { get; set; }

		/// <summary>
		/// Gets or sets the key combinations that invoke an action using the keyboard.
		/// </summary>
		public KeyboardAccelerator? KeyboardAccelerator { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is checked.
		/// </summary>
		public bool IsChecked { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the user can interact with the the menu flyout item.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets a unique identifier (UID) that can be used to save preferences for menu items.
		/// </summary>
		public string? ID { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is in the primary menu flyout.
		/// </summary>
		public bool IsPrimary { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is collapsed.
		/// </summary>
		public bool CollapseLabel { get; set; }

		/// <summary>
		/// Gets or sets the OpacityIcon content of the menu flyout item.
		/// </summary>
		public OpacityIconModel? OpacityIcon { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item will show loading indicator.
		/// </summary>
		public bool ShowLoadingIndicator { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is hidden.
		/// </summary>
		public bool IsHidden { get; set; }
	}
}
