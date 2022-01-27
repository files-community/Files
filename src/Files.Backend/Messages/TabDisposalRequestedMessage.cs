using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Backend.ViewModels.Shell.Tabs;

namespace Files.Backend.Messages
{
    public sealed class TabDisposalRequestedMessage : ValueChangedMessage<TabItemViewModel>
    {
        public TabDisposalRequestedMessage(TabItemViewModel value)
            : base(value)
        {
        }
    }
}
