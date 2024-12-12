// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Controls
{
	public partial class ToolbarSplitButton : SplitButton, IToolbarItemSet
	{
		public ToolbarSplitButton()
		{
			DefaultStyleKey = typeof(ToolbarSplitButton);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
