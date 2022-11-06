using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Files.App.Keyboard
{
	public class KeyboardManager : IKeyboardManager
	{
		private readonly IKeyboardAction None = new NoneAction();

		private IImmutableDictionary<KeyboardActionCodes, KeyboardAction> actions
			= ImmutableDictionary<KeyboardActionCodes, KeyboardAction>.Empty;

		private readonly IDictionary<HotKey, KeyboardAction> hotKeys = new Dictionary<HotKey, KeyboardAction>();

		private UIElement? keyboard;

		public IKeyboardAction this[KeyboardActionCodes code]
			=> actions.TryGetValue(code, out KeyboardAction? action) ? action : None;
		public IKeyboardAction this[HotKey hotKey]
			=> hotKeys.TryGetValue(hotKey, out KeyboardAction? action) ? action : None;

		public HotKeyStatus GetStatus(HotKey hotKey) => hotKey switch
		{
			{ IsNone: true } => HotKeyStatus.Invalid,
			_ when hotKeys.ContainsKey(hotKey) => HotKeyStatus.Used,
			_ => HotKeyStatus.Available,
		};
		public void SetHotKey(KeyboardActionCodes code, HotKey hotKey)
		{
			RemoveHotKey(hotKey);
			RemoveHotKey(this[code].HotKey);
			AddHotKey(actions[code], hotKey);

			void AddHotKey(KeyboardAction action, HotKey hotKey)
			{
				action.HotKey = hotKey;
				hotKeys.Add(hotKey, action);
				AddToKeyboard(hotKey);
			}
			void RemoveHotKey(HotKey hotKey)
			{
				if (hotKeys.TryGetValue(hotKey, out KeyboardAction? action))
				{
					action.HotKey = HotKey.None;
					hotKeys.Remove(hotKey);
					RemoveFromKeyboard(hotKey);
				}
			}
		}

		public void Initialize(IEnumerable<IKeyboardAction> actions)
		{
			this.actions = actions.ToImmutableDictionary(action => action.Code, action => new KeyboardAction(action));
			foreach (var action in actions)
				SetHotKey(action.Code, action.HotKey);
		}

		public void RegisterKeyboard(UIElement keyboard)
		{
			this.keyboard = keyboard;
			keyboard.KeyboardAccelerators.Clear();
			foreach (var hotKey in hotKeys.Keys)
				AddToKeyboard(hotKey);
		}

		public void FillMenu(UIElement element, KeyboardActionCodes code)
		{
			element.KeyboardAccelerators.Clear();

			var hotKey = hotKeys.FirstOrDefault(s => s.Value.Code == code).Key;
			if (!hotKey.IsNone)
			{
				var accelerator = new KeyboardAccelerator
				{
					IsEnabled = false,
					Key = hotKey.Key,
					Modifiers = hotKey.Modifiers,
				};
				element.KeyboardAccelerators.Add(accelerator);
			}
		}

		private void AddToKeyboard(HotKey hotKey)
		{
			if (keyboard is null || hotKey.IsNone)
				return;
			var accelerator = new KeyboardAccelerator
			{
				Key = hotKey.Key,
				Modifiers = hotKey.Modifiers,
			};
			accelerator.Invoked += Accelerator_Invoked;
			keyboard.KeyboardAccelerators.Add(accelerator);
		}
		private void RemoveFromKeyboard(HotKey hotKey)
		{
			if (keyboard is null || hotKey.IsNone)
				return;

			var accelerator = keyboard.KeyboardAccelerators
				.FirstOrDefault(a => a.Key == hotKey.Key && a.Modifiers == hotKey.Modifiers);
			if (accelerator is not null)
			{
				accelerator.Invoked -= Accelerator_Invoked;
				keyboard.KeyboardAccelerators.Remove(accelerator);
			}
		}

		private void Accelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
		{
			var hotKey = new HotKey(sender.Key, sender.Modifiers);
			var action = this[hotKey];
			if (action.Code is not KeyboardActionCodes.None)
			{
				action.Execute();
				e.Handled = true;
			}
		}

		private class NoneAction : IKeyboardAction
		{
			public string Label => string.Empty;
			public string Description => string.Empty;

			public KeyboardActionCodes Code => KeyboardActionCodes.None;
			public HotKey HotKey => HotKey.None;

			public void Execute() {}
		}

		private class KeyboardAction : IKeyboardAction
		{
			private readonly IKeyboardAction action;

			public string Label => action.Label;
			public string Description => action.Label;

			public KeyboardActionCodes Code => action.Code;
			public HotKey HotKey { get; set; }

			public KeyboardAction(IKeyboardAction action) : this(action, action.HotKey)
			{
			}
			public KeyboardAction(IKeyboardAction action, HotKey hotKey)
			{
				this.action = action;
				HotKey = hotKey;
			}

			public void Execute() => action.Execute();
		}
	}
}
