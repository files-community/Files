using Files.Core.ViewModels.Dialogs;
using Files.Core.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Core.Services
{
	/// <summary>
	/// A service to manage dialogs.
	/// </summary>
	public interface IDialogService
	{
		/// <summary>
		/// Gets appropriate dialog with associated <paramref name="viewModel"/>.
		/// </summary>
		/// <typeparam name="TViewModel">The type of view model.</typeparam>
		/// <param name="viewModel">The view model of the dialog.</param>
		/// <returns>A new instance of <see cref="IDialog{TViewModel}"/> with associated <paramref name="viewModel"/>.</returns>
		IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;

		/// <summary>
		/// Creates and shows appropriate dialog derived from associated <paramref name="viewModel"/>.
		/// </summary>
		/// <typeparam name="TViewModel">The type of view model.</typeparam>
		/// <param name="viewModel">The view model of the dialog.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Returns <see cref="DialogResult"/> based on the selected option.</returns>
		Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;
	}
}
