using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Backend.ViewModels.Shell.Tabs;

namespace Files.Backend.Messages
{
    public sealed class TabAddRequestedMessage : ValueChangedMessage<TabItemViewModel>
    {
        public TabAddRequestedMessage(TabItemViewModel value) : base(value)
        {
        }
    }
}
