using System.Collections.Generic;

namespace Files.Services
{
    public interface IStartupSettingsService
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to navigate to a specific location when launching the app.
        /// </summary>
        bool OpenSpecificPageOnStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the default startup location.
        /// </summary>
        string OpenSpecificPageOnStartupPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not continue the last session whenever the app is launched.
        /// </summary>
        bool ContinueLastSessionOnStartUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to open a page when the app is launched.
        /// </summary>
        bool OpenNewTabOnStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not opening the app from the jumplist should open the directory in a new instance.
        /// </summary>
        bool AlwaysOpenNewInstance { get; set; }

        /// <summary>
        /// A list containing all paths to open at startup.
        /// </summary>
        List<string> TabsOnStartupList { get; set; }

        /// <summary>
        /// A list containing all paths to tabs closed on last session.
        /// </summary>
        List<string> LastSessionTabList { get; set; }
    }
}
