// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	// Visual states
	[TemplateVisualState(GroupName = "SideContentVisibilityStates", Name = "NothingToShowTextCollapsed")]
	[TemplateVisualState(GroupName = "SideContentVisibilityStates", Name = "NothingToShowTextVisible")]
	public sealed partial class SamplePanel : Control
	{
		public SamplePanel()
		{
			DefaultStyleKey = typeof(SamplePanel);
		}

		protected override void OnApplyTemplate()
		{
			UpdateVisualStates();

			base.OnApplyTemplate();
		}

		private void UpdateVisualStates()
		{
			VisualStateManager.GoToState(this, SideContent is null ? "NothingToShowTextVisible" : "NothingToShowTextCollapsed", true);
		}
	}
}
