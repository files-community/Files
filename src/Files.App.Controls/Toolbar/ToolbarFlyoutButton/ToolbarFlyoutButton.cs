// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Controls
{
	public partial class ToolbarFlyoutButton : DropDownButton, IToolbarItemSet
	{
		public ToolbarFlyoutButton()
		{
			this.DefaultStyleKey = typeof( ToolbarFlyoutButton );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
