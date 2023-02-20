using System;

namespace Files.App.Commands
{
	public class HotKeyChangedEventArgs : EventArgs
	{
		public required IRichCommand Command { get; init; }

		public HotKey OldHotKey { get; init; } = HotKey.None;
		public HotKey NewHotKey { get; init; } = HotKey.None;
	}
}
