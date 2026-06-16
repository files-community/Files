// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Files.App.Data.Commands
{
	internal sealed partial class CommandManager : ICommandManager
	{
		// Dependency injections

		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();

		// Fields

		private readonly ImmutableArray<IRichCommand> _commands;

		private ImmutableDictionary<HotKey, IRichCommand> _allKeyBindings = new Dictionary<HotKey, IRichCommand>().ToImmutableDictionary();

		public IRichCommand this[CommandCodes code] => _commands[(int)code];
		public IRichCommand this[string code] => Enum.TryParse<CommandCodes>(code, true, out var codeValue) ? this[codeValue] : None;
		public IRichCommand this[HotKey hotKey]
			=> _allKeyBindings.TryGetValue(hotKey with { IsVisible = true }, out var command) ? command
			: _allKeyBindings.TryGetValue(hotKey with { IsVisible = false }, out command) ? command
			: None;

		public CommandManager()
		{
			_commands = CreateCommands();

			ActionsSettingsService.PropertyChanged += (s, e) => { OverwriteKeyBindings(); };

			OverwriteKeyBindings();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IEnumerator<IRichCommand> GetEnumerator() =>
			(_commands as IEnumerable<IRichCommand>).GetEnumerator();

		/// <summary>
		/// Replaces default key binding collection with customized one(s) if exists.
		/// </summary>
		private void OverwriteKeyBindings()
		{
			var allCommands = _commands.OfType<ActionCommand>();

			if (ActionsSettingsService.ActionsV2 is null)
			{
				RestoreKeyBindings(allCommands);
			}
			else
			{
				foreach (var command in allCommands)
				{
					string code = command.Code.ToString();
					var customizedKeyBindings = ActionsSettingsService.ActionsV2.FindAll(x => x.CommandCode == code);

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
				_allKeyBindings = _commands
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);
			}
			catch (ArgumentException ex)
			{
				// The keys are not necessarily all different because they can be set manually in text editor
				// ISSUE: https://github.com/files-community/Files/issues/15331

				var flat = _commands.SelectMany(x => x.HotKeys).Select(x => x.LocalizedLabel);
				var duplicates = flat.GroupBy(x => x).Where(x => x.Count() > 1).Select(group => group.Key);

				foreach (var item in duplicates)
				{
					if (!string.IsNullOrEmpty(item))
					{
						var occurrences = allCommands.Where(x => x.HotKeys.Select(x => x.LocalizedLabel).Contains(item));

						// Restore the defaults for all occurrences in our cache
						RestoreKeyBindings(occurrences);

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
				_allKeyBindings = _commands
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);

				App.Logger.LogInformation(ex, "The app found some keys in different commands are duplicated and are using default key bindings for those commands.");
			}
			catch (Exception ex)
			{
				RestoreKeyBindings(allCommands);

				// Set collection of a set of command code and key bindings to dictionary
				_allKeyBindings = _commands
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);

				App.Logger.LogWarning(ex, "The app is temporarily using default key bindings for all because of a serious error of assigning custom keys.");
			}
		}

		private static void RestoreKeyBindings(IEnumerable<ActionCommand> allCommands)
		{
			foreach (var command in allCommands)
				command.RestoreKeyBindings();
		}
	}
}
