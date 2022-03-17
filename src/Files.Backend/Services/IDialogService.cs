using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IDialogService
    {
        IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;

        Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel, object uiContext = null) where TViewModel : class, INotifyPropertyChanged;
    }
}
