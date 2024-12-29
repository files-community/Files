// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

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
