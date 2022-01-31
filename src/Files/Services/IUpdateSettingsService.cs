using System;
using System.ComponentModel;

namespace Files.Services
{
    public interface IUpdateSettingsService : IBaseSettingsService, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether mandatory updates
        /// should be downloaded only.
        /// </summary>
        bool MandatoryOnly { get; set; }

        /// <summary>
        /// Gets a value indicating whether updates are available.
        /// </summary>
        bool IsUpdateAvailable { get; }

        /// <summary>
        /// Downloads the updates.
        /// </summary>
        void DownloadUpdates();

        /// <summary>
        /// Checks for updates.
        /// </summary>
        void CheckForUpdates();
    }
}
