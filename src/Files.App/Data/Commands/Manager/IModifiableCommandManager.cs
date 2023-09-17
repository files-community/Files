// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Commands
{
	/// <summary>
	/// Represents manager of all globally available modifiable <see cref="IRichCommand"/>s.
	/// </summary>
	public interface IModifiableCommandManager : IEnumerable<IRichCommand>
	{
		IRichCommand this[CommandCodes code] { get; }

		IRichCommand None { get; }

		IRichCommand PasteItem { get; }
		IRichCommand DeleteItem { get; }
	}
}
