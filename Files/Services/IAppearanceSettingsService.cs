using System.ComponentModel;

namespace Files.Services
{
    public interface IAppearanceSettingsService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to move overflow menu items into a sub menu.
        /// </summary>
        bool MoveOverflowMenuItemsToSubMenu { get; set; }
    }
}
