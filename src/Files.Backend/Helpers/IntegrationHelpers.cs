using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Files.Shared.SafetyHelpers;

namespace Files.Backend.Helpers
{
    internal static class IntegrationHelpers
    {
        public static async Task<SafeWrapperResult> ToggleQuickLook(bool switchPreview = false)
        {
            // TODO: Move to a service?
        }
    }
}
