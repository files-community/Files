// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class BreadcrumbBarItem : ContentControl
	{
		// Constants

		private const string TemplatePartName_ItemContentButton = "PART_ItemContentButton";
		private const string TemplatePartName_ItemChevronButton = "PART_ItemChevronButton";
		private const string TemplatePartName_ItemEllipsisDropDownMenuFlyout = "PART_ItemEllipsisDropDownMenuFlyout";
		private const string TemplatePartName_ItemChevronDropDownMenuFlyout = "PART_ItemChevronDropDownMenuFlyout";

		// Fields

		private WeakReference<BreadcrumbBar>? _ownerRef;

		private Button _itemContentButton = null!;
		private Button _itemChevronButton = null!;
		private MenuFlyout _itemEllipsisDropDownMenuFlyout = null!;
		private MenuFlyout _itemChevronDropDownMenuFlyout = null!;

		// Constructor

		public BreadcrumbBarItem()
		{
			DefaultStyleKey = typeof(BreadcrumbBarItem);
		}

		// Methods

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_itemContentButton = GetTemplateChild(TemplatePartName_ItemContentButton) as Button
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ItemContentButton} in the given {nameof(BreadcrumbBarItem)}'s style.");
			_itemChevronButton = GetTemplateChild(TemplatePartName_ItemChevronButton) as Button
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ItemChevronButton} in the given {nameof(BreadcrumbBarItem)}'s style.");
			_itemEllipsisDropDownMenuFlyout = GetTemplateChild(TemplatePartName_ItemEllipsisDropDownMenuFlyout) as MenuFlyout
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ItemEllipsisDropDownMenuFlyout} in the given {nameof(BreadcrumbBarItem)}'s style.");
			_itemChevronDropDownMenuFlyout = GetTemplateChild(TemplatePartName_ItemChevronDropDownMenuFlyout) as MenuFlyout
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ItemChevronDropDownMenuFlyout} in the given {nameof(BreadcrumbBarItem)}'s style.");

			if (IsEllipsis || IsLastItem)
				VisualStateManager.GoToState(this, "ChevronCollapsed", true);

			_itemContentButton.Click += ItemContentButton_Click;
			_itemChevronButton.Click += ItemChevronButton_Click;
			_itemChevronDropDownMenuFlyout.Opening += ChevronDropDownMenuFlyout_Opening;
			_itemChevronDropDownMenuFlyout.Opened += ChevronDropDownMenuFlyout_Opened;
			_itemChevronDropDownMenuFlyout.Closed += ChevronDropDownMenuFlyout_Closed;
		}

		public void OnItemClicked()
		{
			if (_ownerRef is null ||
				!_ownerRef.TryGetTarget(out var breadcrumbBar))
				return;

			if (IsEllipsis)
			{
				// Clear items in the ellipsis flyout
				_itemEllipsisDropDownMenuFlyout.Items.Clear();

				// Populate items in the ellipsis flyout
				for (int index = 0; index < breadcrumbBar.IndexAfterEllipsis; index++)
				{
					if (breadcrumbBar.TryGetElement(index, out var item) && item?.Content is string text)
					{
						var menuFlyoutItem = new MenuFlyoutItem() { Text = text };
						_itemEllipsisDropDownMenuFlyout.Items.Add(menuFlyoutItem);
						menuFlyoutItem.Click += (sender, e) => breadcrumbBar.RaiseItemClickedEvent(item);
					}
				}

				// Open the ellipsis flyout
				FlyoutBase.ShowAttachedFlyout(_itemContentButton);
			}
			else
			{
				// Fire a click event
				breadcrumbBar.RaiseItemClickedEvent(this);
			}
		}

		public void SetOwner(BreadcrumbBar breadcrumbBar)
		{
			_ownerRef = new(breadcrumbBar);
		}
	}
}
