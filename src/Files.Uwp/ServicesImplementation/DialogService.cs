using Files.Backend.ViewModels.Dialogs;
using Files.Backend.Services;
using Files.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Files.ViewModels.Dialogs;
using Files.Dialogs;

namespace Files.Uwp.ServicesImplementation
{
    internal sealed class DialogService : IDialogService
    {
        private readonly Dictionary<Type, Func<ContentDialog>> _dialogs;

        public DialogService()
        {
            this._dialogs = new()
            {
                { typeof(DecompressArchiveDialogViewModel), () => new DecompressArchiveDialog() }
            };
        }

        public IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel)
            where TViewModel : class, INotifyPropertyChanged
        {
            throw new NotImplementedException();
        }

        public Task<DialogResult> ShowDialog<TViewModel>(TViewModel viewModel)
            where TViewModel : class, INotifyPropertyChanged
        {
            throw new NotImplementedException();
        }
    }
}
