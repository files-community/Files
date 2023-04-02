using Files.App.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	/// <summary>
	/// This class is intended to be used with ContextFlyoutItemHelper and ItemModelListToContextFlyoutHelper.
	/// ContextFlyoutItemHelper creates a list of ContextMenuFlyoutItemViewModels representing various commands to be displayed
	/// in a context menu or a command bar. ItemModelListToContextFlyoutHelper has functions that take in said list, and converts
	/// it to a context menu or command bar to be displayed on the window.
	///
	/// Example:
	/// 1) user right clicks
	/// 2) models <- ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems()
	/// 3) menu <- ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(models)
	/// 4) menu.Open()
	/// <see cref="Files.App.Helpers.ContextFlyoutItemHelper"/>
	/// <see cref="Files.App.Helpers.ContextFlyouts.ItemModelListToContextFlyoutHelper"/>
	/// </summary>
	public class ContextMenuFlyoutItemViewModel
	{
		public bool ShowItem { get; set; } = true;

		public ICommand Command { get; set; }

		public object CommandParameter { get; set; }

		public string Glyph { get; set; }

		public string GlyphFontFamilyName { get; set; }

		public string KeyboardAcceleratorTextOverride { get; set; }

		public string Text { get; set; }

		public object Tag { get; set; }

		public ItemType ItemType { get; set; }

		public Func<Task> LoadSubMenuAction { get; set; }

		public List<ContextMenuFlyoutItemViewModel> Items { get; set; }

		public BitmapImage BitmapIcon { get; set; }

		/// <summary>
		/// Only show the item when the shift key is held
		/// </summary>
		public bool ShowOnShift { get; set; }

		/// <summary>
		/// Only show when one item is selected
		/// </summary>
		public bool SingleItemOnly { get; set; }

		/// <summary>
		/// True if the item is shown in the recycle bin
		/// </summary>
		public bool ShowInRecycleBin { get; set; }

		/// <summary>
		/// True if the item is shown in search page
		/// </summary>
		public bool ShowInSearchPage { get; set; }

		/// <summary>
		/// True if the item is shown in FTP page
		/// </summary>
		public bool ShowInFtpPage { get; set; }

		/// <summary>
		/// True if the item is shown in ZIP archive page
		/// </summary>
		public bool ShowInZipPage { get; set; }

		public KeyboardAccelerator KeyboardAccelerator { get; set; }

		public bool IsChecked { get; set; }

		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// A unique identifier that can be used to save preferences for menu items
		/// </summary>
		public string ID { get; set; }

		public bool IsPrimary { get; set; }

		public bool CollapseLabel { get; set; }

		public OpacityIconModel OpacityIcon { get; set; }

		public bool ShowLoadingIndicator { get; set; }

		public bool IsHidden { get; set; }
	}

	public enum ItemType
	{
		Item,
		Separator,
		Toggle,
		SplitButton,
	}

	public struct OpacityIconModel
	{
		public string OpacityIconStyle { get; set; }

		public OpacityIcon ToOpacityIcon() => new()
		{
			Style = (Style)Application.Current.Resources[OpacityIconStyle],
		};

		public bool IsValid => !string.IsNullOrEmpty(OpacityIconStyle);
	}
}
