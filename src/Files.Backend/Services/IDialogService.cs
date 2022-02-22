using Files.Backend.Models;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IDialogService
    {
        IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;

        Task<DialogResult> ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;
    }
}
