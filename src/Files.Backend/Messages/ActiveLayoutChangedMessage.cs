using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Backend.ViewModels.Layouts;

namespace Files.Backend.Messages
{
    public sealed class ActiveLayoutChangedMessage : ValueChangedMessage<BaseLayoutViewModel>
    {
        public ActiveLayoutChangedMessage(BaseLayoutViewModel value)
            : base(value)
        {
        }
    }
}
