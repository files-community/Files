using Files.Backend.Models;
using Files.Backend.ViewModels.Dialogs;
using Files.Dialogs;
using Files.Shared.Enums;
using Files.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Files.ServicesImplementation
{
    internal sealed class DialogService
    {
        private readonly Dictionary<Type, Func<ContentDialog>> _dialogs;

        public DialogService()
        {
            this._dialogs = new()
            {
                { typeof(AddItemDialogViewModel), () => new AddItemDialog() },
                { typeof(CredentialDialogViewModel), () => new CredentialDialog() },
                { typeof(DecompressArchiveDialogViewModel), () => new DecompressArchiveDialog() },
                { typeof(ElevateConfirmDialogViewModel), () => new ElevateConfirmDialog() },
                { typeof(FilesystemOperationDialogViewModel), () => new FilesystemOperationDialog() }
            };
        }

        public IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel)
            where TViewModel : class, INotifyPropertyChanged
        {
            if (!_dialogs.TryGetValue(typeof(TViewModel), out var initializer))
            {
                throw new ArgumentException($"{typeof(TViewModel)} does not have an appropriate dialog associated with it.");
            }

            var contentDialog = initializer();

            if (contentDialog is not IDialog<TViewModel> dialog)
            {
                throw new NotSupportedException($"The dialog does not implement {typeof(IDialog<TViewModel>)}.");
            }

            dialog.ViewModel = viewModel;

            return dialog;
        }

        public Task<DialogResult> ShowDialog<TViewModel>(TViewModel viewModel)
            where TViewModel : class, INotifyPropertyChanged
        {
            return GetDialog<TViewModel>(viewModel).ShowAsync();
        }
    }
}
