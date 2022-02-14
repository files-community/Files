using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Backend.ViewModels.Shell.Tabs;

namespace Files.Backend.Messages
{
    public sealed class TabInstanceChangedMessage : ValueChangedMessage<TabItemViewModel>
    {
        public TabInstanceChangedMessage(TabItemViewModel value)
            : base(value)
        {
        }
    }
}
