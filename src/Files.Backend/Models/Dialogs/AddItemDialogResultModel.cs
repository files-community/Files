// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Enums;
using Files.Shared;

namespace Files.Backend.Models.Dialogs
{
	public sealed class AddItemDialogResultModel
	{
		public AddItemDialogItemType ItemType { get; set; }

		public ShellNewEntry? ItemInfo { get; set; }
	}
}
