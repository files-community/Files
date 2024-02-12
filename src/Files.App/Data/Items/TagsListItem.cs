// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public class TagsListItem
	{
		public bool IsTag
			=> this is TagItem;

		public TagItem? AsTag
			=> this as TagItem;

		public bool IsFlyout
			=> this is FlyoutItem;

		public FlyoutItem? AsFlyout
			=> this as FlyoutItem;
	}

	public class TagItem : TagsListItem
	{
		public TagViewModel Tag { get; set; }

		public TagItem(TagViewModel tag)
		{
			Tag = tag;
		}
	}

	public class FlyoutItem : TagsListItem
	{
		public MenuFlyout Flyout { get; set; }

		public FlyoutItem(MenuFlyout flyout)
		{
			Flyout = flyout;
		}
	}
}
