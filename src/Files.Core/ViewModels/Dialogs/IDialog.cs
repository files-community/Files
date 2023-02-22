using Files.Core.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Core.ViewModels.Dialogs
{
	public interface IDialog<TViewModel>
		where TViewModel : class, INotifyPropertyChanged
	{
		TViewModel ViewModel { get; set; }

		Task<DialogResult> ShowAsync();
	}
}
