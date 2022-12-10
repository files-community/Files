using Files.App.DataModels;

namespace Files.App.Commands
{
	public interface IHotKeyManager
	{
		event HotKeyChangedEventHandler? HotKeyChanged;

		CommandCodes this[HotKey hotKey] { get; set; }
		HotKey this[CommandCodes commandCode] { get; set; }
	}
}
