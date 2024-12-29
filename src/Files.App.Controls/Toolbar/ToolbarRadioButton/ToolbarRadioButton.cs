// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class ToolbarRadioButton : RadioButton, IToolbarItemSet
	{
		public ToolbarRadioButton()
		{
			DefaultStyleKey = typeof( ToolbarRadioButton );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
