using Files.Dialogs;
using Files.Filesystem;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    public static class DynamicDialogFactory
    {
        public static DynamicDialog GetFor_PropertySaveDialog()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "PropertySaveErrorDialog/Title".GetLocalized(),
                SubtitleText = "PropertySaveErrorMessage/Text".GetLocalized(), // We can use subtitle here as our content
                PrimaryButtonText = "PropertySaveErrorDialog/PrimaryButtonText".GetLocalized(),
                SecondaryButtonText = "PropertySaveErrorDialog/SecondaryButtonText".GetLocalized(),
                CloseButtonText= "PropertySaveErrorDialog/CloseButtonText".GetLocalized(),
                DynamicButtons = DynamicButtons.Primary | DynamicButtons.Secondary | DynamicButtons.Cancel
            });
            return dialog;
        }

        public static DynamicDialog GetFor_ConsentDialog()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "WelcomeDialog/Title".GetLocalized(),
                SubtitleText = "WelcomeDialogTextBlock/Text".GetLocalized(), // We can use subtitle here as our content
                PrimaryButtonText = "WelcomeDialog/PrimaryButtonText".GetLocalized(),
                PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
                DynamicButtons = DynamicButtons.Primary
            });
            return dialog;
        }
    }
}
