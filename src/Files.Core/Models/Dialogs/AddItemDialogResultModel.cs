using Files.Core.Enums;
using Files.Core;

namespace Files.Core.Models.Dialogs
{
	public sealed class AddItemDialogResultModel
	{
		public AddItemDialogItemType ItemType { get; set; }

		public ShellNewEntry? ItemInfo { get; set; }
	}
}
