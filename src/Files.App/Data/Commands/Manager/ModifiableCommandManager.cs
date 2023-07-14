// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Immutable;

namespace Files.App.Data.Commands
{
	internal class ModifiableCommandManager : IModifiableCommandManager
	{
		private static readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly IImmutableDictionary<CommandCodes, IRichCommand> ModifiableCommands;

		public IRichCommand this[CommandCodes code] => ModifiableCommands.TryGetValue(code, out var command) ? command : None;

		public IRichCommand None => ModifiableCommands[CommandCodes.None];
		public IRichCommand PasteItem => ModifiableCommands[CommandCodes.PasteItem];
		public IRichCommand DeleteItem => ModifiableCommands[CommandCodes.DeleteItem];

		public ModifiableCommandManager()
		{
			ModifiableCommands = CreateModifiableCommands().ToImmutableDictionary();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<IRichCommand> GetEnumerator() => ModifiableCommands.Values.GetEnumerator();

		private static IDictionary<CommandCodes, IRichCommand> CreateModifiableCommands() => new Dictionary<CommandCodes, IRichCommand>
		{
			[CommandCodes.None] = new NoneCommand(),
			[CommandCodes.PasteItem] = new ModifiableCommand(Commands.PasteItem, new() {
				{ KeyModifiers.Shift,  Commands.PasteItemToSelection }
			}),
			[CommandCodes.DeleteItem] = new ModifiableCommand(Commands.DeleteItem, new() {
				{ KeyModifiers.Shift,  Commands.DeleteItemPermanently }
			}),
		};
	}
}
