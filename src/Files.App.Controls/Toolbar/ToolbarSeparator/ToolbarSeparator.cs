// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Controls
{
	public partial class ToolbarSeparator : Control , IToolbarItemSet
	{
		public ToolbarSeparator()
		{
			DefaultStyleKey = typeof(ToolbarSeparator);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
