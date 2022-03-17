using Files.Backend.ViewModels.Dialogs;
using Files.Dialogs;
using Files.Shared.Enums;
using Files.Uwp.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;

namespace Files.Extensions
{
    public static class DialogExtensions
    {
        public static async Task<DialogResult> TryShowAsync<TViewModel>(this IDialog<TViewModel> dialog, object context = null)
            where TViewModel : class, INotifyPropertyChanged
        {
            try
            {
                if (context is UIContext uiContext)
                {
                    ((IDialogWithUIContext)dialog).Context = uiContext;
                }
                else
                {
                    ((IDialogWithUIContext)dialog).Context = (WindowManagementHelpers.GetAnyWindow() is AppWindow aw) ? aw.UIContext : Window.Current.Content.XamlRoot.UIContext;
                }
                return await dialog.ShowAsync();
            }
            catch
            {
                // Another dialog is already open
                return DialogResult.None;
            }
        }
    }
}
