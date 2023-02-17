using System.Collections.Generic;

namespace Files.App.Commands
{
	public interface ICommandManager : IEnumerable<IRichCommand>
	{
		IRichCommand this[CommandCodes code] { get; }

		IRichCommand None { get; }

		IRichCommand ShowHiddenItems { get; }
		IRichCommand ShowFileExtensions { get; }
	}
}
