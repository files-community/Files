﻿using System;
using System.Collections.Generic;

namespace Files.App.Commands
{
	public interface ICommandManager : IEnumerable<IRichCommand>
	{
		event EventHandler<HotKeyChangedEventArgs>? HotKeyChanged;

		IRichCommand this[CommandCodes code] { get; }
		IRichCommand this[HotKey customHotKey] { get; }

		IRichCommand None { get; }

		IRichCommand OpenHelp { get; }
		IRichCommand ToggleFullScreen { get; }

		IRichCommand ToggleShowHiddenItems { get; }
		IRichCommand ToggleShowFileExtensions { get; }
	}
}
