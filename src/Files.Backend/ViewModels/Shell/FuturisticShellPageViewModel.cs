using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Files.Backend.ViewModels.Layouts;

namespace Files.Backend.ViewModels.Shell
{
    public sealed class FuturisticShellPageViewModel : ObservableObject
    {
        public IMessenger Messenger { get; }

        public BaseLayoutViewModel ActiveLayoutViewModel { get; private set; } // TODO(i): Send a message on active layout changed ActiveLayoutChangedMessage

        public BaseLayoutViewModel LeftLayoutViewModel { get; private set; }

        public BaseLayoutViewModel RightLayoutViewModel { get; private set; }

        public FuturisticShellPageViewModel(IMessenger messenger)
        {
            this.Messenger = messenger;
        }
    }
}
