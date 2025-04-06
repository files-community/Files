// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.UITests.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Files.App.UITests.Views
{
	public sealed partial class BreadcrumbBarPage : Page
	{
		private readonly ObservableCollection<BreadcrumbBarItemModel> DummyItems;

		[GeneratedDependencyProperty]
		private partial string? ClickedItemName { get; set; }

		[GeneratedDependencyProperty]
		private partial string? ClickedItemIndex { get; set; }

		[GeneratedDependencyProperty]
		private partial bool IsRTLEnabled { get; set; }

		[GeneratedDependencyProperty]
		private partial FlowDirection BreadcrumbBar1FlowDirection { get; set; }

		public BreadcrumbBarPage()
		{
			InitializeComponent();

			DummyItems =
			[
				new("Local Disk (C:)"),
				new("Users"),
				new("me"),
				new("OneDrive"),
				new("Desktop"),
				new("Folder1"),
				new("Folder2"),
			];
		}

		private void BreadcrumbBar1_ItemClicked(Controls.BreadcrumbBar sender, Controls.BreadcrumbBarItemClickedEventArgs args)
		{
			if (args.IsRootItem)
			{
				ClickedItemName = "Home";
				ClickedItemIndex = "Root";
			}
			else
			{
				ClickedItemName = DummyItems[args.Index].Text;
				ClickedItemIndex = $"{args.Index}";
			}
		}

		private void BreadcrumbBar1_ItemDropDownFlyoutOpening(object sender, BreadcrumbBarItemDropDownFlyoutEventArgs e)
		{
			e.Flyout.Items.Add(new MenuFlyoutItem { Icon = new FontIcon() { Glyph = "\uE8B7" }, Text = "Item 1" });
			e.Flyout.Items.Add(new MenuFlyoutItem { Icon = new FontIcon() { Glyph = "\uE8B7" }, Text = "Item 2" });
			e.Flyout.Items.Add(new MenuFlyoutItem { Icon = new FontIcon() { Glyph = "\uE8B7" }, Text = "Item 3" });
		}

		private void BreadcrumbBar1_ItemDropDownFlyoutClosed(object sender, BreadcrumbBarItemDropDownFlyoutEventArgs e)
		{
			e.Flyout.Items.Clear();
		}

		partial void OnIsRTLEnabledChanged(bool newValue)
		{
			BreadcrumbBar1FlowDirection = IsRTLEnabled ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		}
	}
}
