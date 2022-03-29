using System.ComponentModel;
using System.Threading.Tasks;
using Files.Shared.Enums;

namespace Files.Backend.ViewModels.Dialogs
{
	public interface IDialog<TViewModel>
		where TViewModel : class, INotifyPropertyChanged
	{
		TViewModel ViewModel { get; set; }

		Task<DialogResult> ShowAsync();
	}
}
