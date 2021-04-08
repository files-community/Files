using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Files.Helpers
{
    public static class UIHelpers
    {
        public static bool IsAnyContentDialogOpen()
        {
            var openedPopups = VisualTreeHelper.GetOpenPopups(Window.Current);
            return openedPopups.Any(popup => popup.Child is ContentDialog);
        }
    }
}