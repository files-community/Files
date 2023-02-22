using Files.Core.ViewModels.Dialogs;
using Files.Core.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Core.Extensions
{
	public static class DialogExtensions
	{
		public static Task<DialogResult> TryShowAsync<TViewModel>(this IDialog<TViewModel> dialog)
			where TViewModel : class, INotifyPropertyChanged
		{
			try
			{
				return dialog.ShowAsync();
			}
			catch
			{
				// Another dialog is already open
				return Task.FromResult(DialogResult.None);
			}
		}
	}
}
