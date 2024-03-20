﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public sealed class QuickAccessCardEventArgs : EventArgs
	{
		public LocationItem? Item { get; set; }
	}

	public sealed class QuickAccessCardInvokedEventArgs : EventArgs
	{
		public string? Path { get; set; }
	}

	public sealed class ModifyQuickAccessEventArgs : EventArgs
	{
		public string[]? Paths { get; set; }
		public ShellFileItem[]? Items { get; set; }
		public bool Add;
		public bool Pin = true;
		public bool Reset = false;
		public bool Reorder = false;

		public ModifyQuickAccessEventArgs(string[] paths, bool add)
		{
			Paths = paths;
			Add = add;
		}

		public ModifyQuickAccessEventArgs(ShellFileItem[] items, bool add)
		{
			Paths = items.Select(x => x.FilePath).ToArray();
			Items = items;
			Add = add;
		}
	}
}
