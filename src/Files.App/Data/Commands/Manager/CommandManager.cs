// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Actions;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Files.App.Data.Commands
{
	internal sealed partial class CommandManager : ICommandManager
	{
		// Dependency injections

		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();

		// Fields

		private ImmutableDictionary<HotKey, IRichCommand> _allKeyBindings = new Dictionary<HotKey, IRichCommand>().ToImmutableDictionary();

		public IRichCommand this[CommandCodes code] => commands.TryGetValue(code, out var command) ? command : None;
		public IRichCommand this[string code]
		{
			get
			{
				try
				{
					return commands[Enum.Parse<CommandCodes>(code, true)];
				}
				catch
				{
					return None;
				}
			}
		}
		public IRichCommand this[HotKey hotKey]
			=> _allKeyBindings.TryGetValue(hotKey with { IsVisible = true }, out var command) ? command
			: _allKeyBindings.TryGetValue(hotKey with { IsVisible = false }, out command) ? command
			: None;

		public CommandManager()
		{
			commands = CreateActions()
				.Select(action => new ActionCommand(this, action.Key, action.Value))
				.Cast<IRichCommand>()
				.Append(new NoneCommand())
				.ToFrozenDictionary(command => command.Code);

			ActionsSettingsService.PropertyChanged += (s, e) => { OverwriteKeyBindings(); };

			OverwriteKeyBindings();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IEnumerator<IRichCommand> GetEnumerator() =>
			(commands.Values as IEnumerable<IRichCommand>).GetEnumerator();

		/// <summary>
		/// Replaces default key binding collection with customized one(s) if exists.
		/// </summary>
		private void OverwriteKeyBindings()
		{
			var allCommands = commands.Values.OfType<ActionCommand>();

			if (ActionsSettingsService.ActionsV2 is null)
			{
				allCommands.ForEach(x => x.RestoreKeyBindings());
			}
			else
			{
				foreach (var command in allCommands)
				{
					var customizedKeyBindings = ActionsSettingsService.ActionsV2.FindAll(x => x.CommandCode == command.Code.ToString());

					if (customizedKeyBindings.IsEmpty())
					{
						// Could not find customized key bindings for the command
						command.RestoreKeyBindings();
					}
					else if (customizedKeyBindings.Count == 1 && customizedKeyBindings[0].KeyBinding == string.Empty)
					{
						// Do not assign any key binding even though there're default keys pre-defined
						command.OverwriteKeyBindings(HotKeyCollection.Empty);
					}
					else
					{
						var keyBindings = new HotKeyCollection(customizedKeyBindings.Select(x => HotKey.Parse(x.KeyBinding, false)));
						command.OverwriteKeyBindings(keyBindings);
					}
				}
			}

			try
			{
				// Set collection of a set of command code and key bindings to dictionary
				_allKeyBindings = commands.Values
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);
			}
			catch (ArgumentException ex)
			{
				// The keys are not necessarily all different because they can be set manually in text editor
				// ISSUE: https://github.com/files-community/Files/issues/15331

				var flat = commands.Values.SelectMany(x => x.HotKeys).Select(x => x.LocalizedLabel);
				var duplicates = flat.GroupBy(x => x).Where(x => x.Count() > 1).Select(group => group.Key);

				foreach (var item in duplicates)
				{
					if (!string.IsNullOrEmpty(item))
					{
						var occurrences = allCommands.Where(x => x.HotKeys.Select(x => x.LocalizedLabel).Contains(item));

						// Restore the defaults for all occurrences in our cache
						occurrences.ForEach(x => x.RestoreKeyBindings());

						// Get all customized key bindings from user settings json
						var actions =
							ActionsSettingsService.ActionsV2 is not null
								? new List<ActionWithParameterItem>(ActionsSettingsService.ActionsV2)
								: [];

						// Remove the duplicated key binding from user settings JSON file
						actions.RemoveAll(x => x.KeyBinding.Contains(item));

						// Reset
						ActionsSettingsService.ActionsV2 = actions;
					}
				}

				// Set collection of a set of command code and key bindings to dictionary
				_allKeyBindings = commands.Values
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);

				App.Logger.LogInformation(ex, "The app found some keys in different commands are duplicated and are using default key bindings for those commands.");
			}
			catch (Exception ex)
			{
				allCommands.ForEach(x => x.RestoreKeyBindings());

				// Set collection of a set of command code and key bindings to dictionary
				_allKeyBindings = commands.Values
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);

				App.Logger.LogWarning(ex, "The app is temporarily using default key bindings for all because of a serious error of assigning custom keys.");
			}
		}

		public static HotKeyCollection GetDefaultKeyBindings(IAction action)
		{
			return new(action.HotKey, action.SecondHotKey, action.ThirdHotKey, action.MediaHotKey);
		}
	}
}
