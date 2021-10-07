namespace Files.Services
{
    public interface IAppearanceSettingsService
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to move overflow menu items into a sub menu.
        /// </summary>
        bool MoveOverflowMenuItemsToSubMenu { get; set; }
    }
}
