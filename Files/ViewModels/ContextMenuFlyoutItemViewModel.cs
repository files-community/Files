using Files.Filesystem;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels
{
    public class ContextMenuFlyoutItemViewModel
    {
        public Func<bool> CheckShowItem { get; set; }
        public ICommand Command {get; set; }
        public object CommandParameter { get; set; }
        public string Glyph { get; set; }
        public string GlyphFontFamilyName { get; set; }
        public string Text { get; set; }
        public object Tag { get; set; }
        public ItemType ItemType { get; set; }
        public bool IsSubItem { get; set; }
        public List<ContextMenuFlyoutItemViewModel> Items { get; set; } = new List<ContextMenuFlyoutItemViewModel>();
        public BitmapImage BitmapIcon { get; set; }
        /// <summary>
        /// Only show the item when the shift key is held
        /// </summary>
        public bool ShowOnShift { get; set; }
        /// <summary>
        /// Only show when one item is selected
        /// </summary>
        public bool SingleItemOnly { get; set; }
        public KeyboardAccelerator KeyboardAccelerator { get; set; }
    }

    public enum ItemType
    {
        Item,
        Separator,
    }
}
