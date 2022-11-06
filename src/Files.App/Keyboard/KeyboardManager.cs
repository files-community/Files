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

		private readonly IDictionary<ShortKey, KeyboardAction> shortKeys = new Dictionary<ShortKey, KeyboardAction>();

		private UIElement? keyboard;

		public IKeyboardAction this[KeyboardActionCodes code]
			=> actions.TryGetValue(code, out KeyboardAction? action) ? action : None;
		public IKeyboardAction this[ShortKey shortKey]
			=> shortKeys.TryGetValue(shortKey, out KeyboardAction? action) ? action : None;

		public ShortKeyStatus GetStatus(ShortKey shortKey) => shortKey switch
		{
			{ IsNone: true } => ShortKeyStatus.Invalid,
			_ when shortKeys.ContainsKey(shortKey) => ShortKeyStatus.Used,
			_ => ShortKeyStatus.Available,
		};
		public void SetShortKey(KeyboardActionCodes code, ShortKey shortKey)
		{
			RemoveShortKey(shortKey);
			RemoveShortKey(this[code].ShortKey);
			AddShortKey(actions[code], shortKey);

			void AddShortKey(KeyboardAction action, ShortKey shortKey)
			{
				action.ShortKey = shortKey;
				shortKeys.Add(shortKey, action);
				AddToKeyboard(shortKey);
			}
			void RemoveShortKey(ShortKey shortKey)
			{
				if (shortKeys.TryGetValue(shortKey, out KeyboardAction? action))
				{
					action.ShortKey = ShortKey.None;
					shortKeys.Remove(shortKey);
					RemoveFromKeyboard(shortKey);
				}
			}
		}

		public void Initialize(IEnumerable<IKeyboardAction> actions)
		{
			this.actions = actions.ToImmutableDictionary(action => action.Code, action => new KeyboardAction(action));
			foreach (var action in actions)
				SetShortKey(action.Code, action.ShortKey);
		}

		public void RegisterKeyboard(UIElement keyboard)
		{
			this.keyboard = keyboard;
			keyboard.KeyboardAccelerators.Clear();
			foreach (var shortKey in shortKeys.Keys)
				AddToKeyboard(shortKey);
		}

		public void FillMenu(UIElement element, KeyboardActionCodes code)
		{
			element.KeyboardAccelerators.Clear();

			var shortKey = shortKeys.FirstOrDefault(s => s.Value.Code == code).Key;
			if (!shortKey.IsNone)
			{
				var accelerator = new KeyboardAccelerator
				{
					IsEnabled = false,
					Key = shortKey.Key,
					Modifiers = shortKey.Modifiers,
				};
				element.KeyboardAccelerators.Add(accelerator);
			}
		}

		private void AddToKeyboard(ShortKey shortKey)
		{
			if (keyboard is null || shortKey.IsNone)
				return;
			var accelerator = new KeyboardAccelerator
			{
				Key = shortKey.Key,
				Modifiers = shortKey.Modifiers,
			};
			accelerator.Invoked += Accelerator_Invoked;
			keyboard.KeyboardAccelerators.Add(accelerator);
		}
		private void RemoveFromKeyboard(ShortKey shortKey)
		{
			if (keyboard is null || shortKey.IsNone)
				return;

			var accelerator = keyboard.KeyboardAccelerators
				.FirstOrDefault(a => a.Key == shortKey.Key && a.Modifiers == shortKey.Modifiers);
			if (accelerator is not null)
			{
				accelerator.Invoked -= Accelerator_Invoked;
				keyboard.KeyboardAccelerators.Remove(accelerator);
			}
		}

		private void Accelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
		{
			var shortKey = new ShortKey(sender.Key, sender.Modifiers);
			var action = this[shortKey];
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
			public ShortKey ShortKey => ShortKey.None;

			public void Execute() {}
		}

		private class KeyboardAction : IKeyboardAction
		{
			private readonly IKeyboardAction action;

			public string Label => action.Label;
			public string Description => action.Label;

			public KeyboardActionCodes Code => action.Code;
			public ShortKey ShortKey { get; set; }

			public KeyboardAction(IKeyboardAction action) : this(action, action.ShortKey)
			{
			}
			public KeyboardAction(IKeyboardAction action, ShortKey shortKey)
			{
				this.action = action;
				ShortKey = shortKey;
			}

			public void Execute() => action.Execute();
		}
	}
}
