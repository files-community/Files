namespace Files.Services
{
    public interface IPreferencesSettingsService
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to show the delete confirmation dialog when deleting items.
        /// </summary>
        bool ShowConfirmDeleteDialog { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to open folders in new tab.
        /// </summary>
        bool OpenFoldersInNewTab { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to open zip archives in Files.
        /// </summary>
        bool OpenArchivesInFiles { get; set; }
    }
}
