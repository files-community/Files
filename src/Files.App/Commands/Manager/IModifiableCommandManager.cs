// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Commands
{
	public interface IModifiableCommandManager : IEnumerable<IRichCommand>
	{
		IRichCommand None { get; }

		IRichCommand PasteItem { get; }
		IRichCommand DeleteItem { get; }
	}
}
