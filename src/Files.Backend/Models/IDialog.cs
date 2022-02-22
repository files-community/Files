using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Backend.Models
{
    public interface IDialog<TViewModel>
        where TViewModel : class, INotifyPropertyChanged
    {
        TViewModel ViewModel { get; set; }

        Task<DialogResult> ShowAsync();
    }
}
