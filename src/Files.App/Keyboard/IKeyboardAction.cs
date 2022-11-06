namespace Files.App.Keyboard
{
	public interface IKeyboardAction
	{
		string Label { get; }
		string Description { get; }

		KeyboardActionCodes Code { get; }
		HotKey HotKey { get; }

		void Execute();
	}
}
