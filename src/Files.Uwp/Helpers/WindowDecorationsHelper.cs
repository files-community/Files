using System;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;

namespace Files.Uwp.Helpers
{
    public static class WindowDecorationsHelper
    {
        public static bool IsWindowDecorationsAllowed { get; private set; }

        public static void RequestWindowDecorationsAccess()
        {
            bool canSetAppWindowTitlebarVisibility = ApiInformation.IsMethodPresent("Windows.UI.WindowManagement.AppWindowTitleBar", "SetPreferredVisibility");

            if (canSetAppWindowTitlebarVisibility)
            {
                string attestation = $"{Package.Current.Id.PublisherId} has registered their use of com.microsoft.windows.windowdecorations with Microsoft and agrees to the terms of use.";

                string token = Package.Current.Id.Name switch
                {
                    "Files" => "xnYLj99c3vN9jFCZiDC6Rg==",
                    "FilesDev" => "Yoz9y1X66micskUKhFrJ5A==",
                    "49306atecsolution.FilesUWP" => "+cyyj4JZUyZrBQ2eqDQGeA==",
                    _ => String.Empty,
                };

                LimitedAccessFeatureRequestResult accessResult = LimitedAccessFeatures.TryUnlockFeature("com.microsoft.windows.windowdecorations", token, attestation);
                if (accessResult.Status == LimitedAccessFeatureStatus.Available)
                {
                    IsWindowDecorationsAllowed = true;
                }
                else
                {
                    IsWindowDecorationsAllowed = false;
                }
            }
            else
            {
                IsWindowDecorationsAllowed = false;
            }
        }
    }
}