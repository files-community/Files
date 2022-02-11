using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Files.Backend.Messages;
using Files.Backend.Models.Icons;

namespace Files.Backend.ViewModels.Shell.Tabs
{
    public sealed class TabItemViewModel : ObservableObject, IDisposable
    {
        public FuturisticShellPageViewModel TabShell { get; }

        private string _Header;
        public string Header
        {
            get => _Header;
            set => SetProperty(ref _Header, value);
        }

        private IconModel _TabIcon;
        public IconModel TabIcon
        {
            get => _TabIcon;
            set => SetProperty(ref _TabIcon, value);
        }

        private bool isLoading;

        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        private bool allowStorageItemDrop;

        public bool AllowStorageItemDrop
        {
            get => allowStorageItemDrop;
            set => SetProperty(ref allowStorageItemDrop, value);
        }

        public IRelayCommand OpenNewTabCommand { get; }

        public TabItemViewModel(FuturisticShellPageViewModel tabShell)
        {
            this.TabShell = tabShell;

            this.OpenNewTabCommand = new RelayCommand(OpenNewTab);
        }

        public void SetDisplayInformation(string header, IconModel icon)
        {
            Header = header;
            TabIcon = icon;
        }

        public void PutToSleep()
        {

        }

        private void OpenNewTab()
        {

        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.Send(new TabDisposalRequestedMessage(this));
        }
    }
}
