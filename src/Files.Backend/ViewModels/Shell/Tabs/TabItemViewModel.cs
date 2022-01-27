using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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

        public TabItemViewModel(FuturisticShellPageViewModel tabShell)
        {
            this.TabShell = tabShell;
        }

        public void PutToSleep()
        {

        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.Send(new TabDisposalRequestedMessage(this));
        }
    }
}
