// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.DataModels.NavigationControlItems
{
	public class WslDistroItem : INavigationControlItem
	{
		public string Text { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTipText = Path.Contains('?', StringComparison.Ordinal) ? Text : Path;
			}
		}

		public string ToolTipText { get; private set; }

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.LinuxDistro;

		public Uri Logo { get; set; }

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
	}
}
