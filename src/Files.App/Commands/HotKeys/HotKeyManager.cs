using Files.App.DataModels;
using System.Collections.Generic;
using System.Linq;

namespace Files.App.Commands
{
	public class HotKeyManager : IHotKeyManager
	{
		private readonly IDictionary<HotKey, CommandCodes> hotKeys = new Dictionary<HotKey, CommandCodes>();

		public event HotKeyChangedEventHandler? HotKeyChanged;

		public CommandCodes this[HotKey hotKey]
		{
			get => hotKeys.TryGetValue(hotKey, out CommandCodes CommandCode) ? CommandCode : CommandCodes.None;
			set
			{
				var oldCommandCode = this[hotKey];
				if (oldCommandCode == value)
					return;

				if (value is CommandCodes.None)
					hotKeys.Remove(hotKey);
				else if (oldCommandCode is CommandCodes.None)
					hotKeys[hotKey] = value;
				else
					hotKeys.Add(hotKey, value);

				var args = new HotKeyChangedEventArgs
				{
					OldHotKey = hotKey,
					NewHotKey = hotKey,
					OldCommandCode = oldCommandCode,
					NewCommandCode = value,
				};
				HotKeyChanged?.Invoke(this, args);
			}
		}

		public HotKey this[CommandCodes CommandCode]
		{
			get => hotKeys.FirstOrDefault(hotKey => hotKey.Value == CommandCode).Key;
			set
			{
				var oldHotKey = this[CommandCode];
				if (oldHotKey == value)
					return;

				if (value.IsNone)
					hotKeys.Remove(oldHotKey);
				else if (!oldHotKey.IsNone)
					hotKeys[oldHotKey] = CommandCode;
				else
					hotKeys.Add(value, CommandCode);

				var args = new HotKeyChangedEventArgs
				{
					OldHotKey = oldHotKey,
					NewHotKey = value,
					OldCommandCode = CommandCode,
					NewCommandCode = CommandCode,
				};
				HotKeyChanged?.Invoke(this, args);
			}
		}

		public void Initialize(IDictionary<HotKey, CommandCodes> hotKeys)
		{
			this.hotKeys.Clear();
			foreach (var hotKey in hotKeys)
				this.hotKeys.Add(hotKey.Key, hotKey.Value);
		}
	}
}
