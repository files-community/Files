using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Core.ViewModels.Layouts;

namespace Files.Core.Messages
{
	public sealed class ActiveLayoutChangedMessage : ValueChangedMessage<BaseLayoutViewModel>
	{
		public ActiveLayoutChangedMessage(BaseLayoutViewModel value)
			: base(value)
		{
		}
	}
}
