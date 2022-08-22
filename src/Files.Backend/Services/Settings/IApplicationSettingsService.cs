using System;
using System.Collections.Generic;

namespace Files.Backend.Services.Settings
{
    public interface IApplicationSettingsService : IBaseSettingsService
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not the user was prompted to review the app.
        /// </summary>
        bool WasPromptedToReview { get; set; }

    }
}
