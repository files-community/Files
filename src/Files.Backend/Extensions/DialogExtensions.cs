using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Backend.Extensions
{
    public static class DialogExtensions
    {
        public static async Task<DialogResult> TryShowAsync<TViewModel>(this IDialog<TViewModel> dialog)
            where TViewModel : class, INotifyPropertyChanged
        {
            try
            {
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
