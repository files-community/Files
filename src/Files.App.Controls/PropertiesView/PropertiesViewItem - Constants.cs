using System;
using System.Collections.Generic;
using System.Text;




namespace Files.App.Controls.Primitives
{
	[TemplateVisualState( Name = HorizontalStateName , GroupName = OrientationStateGroupName )]
	[TemplateVisualState( Name = VerticalStateName , GroupName = OrientationStateGroupName )]

	[TemplateVisualState( Name = NotEditableStateName , GroupName = EditableStateGroupName )]
	[TemplateVisualState( Name = EditableStateName , GroupName = EditableStateGroupName )]




	public partial class PropertiesViewItem : Control
	{
		internal const string OrientationStateGroupName = "OrientationStateGroup";
		internal const string EditableStateGroupName = "EditableStateGroup";

		internal const string HorizontalStateName = "HorizontalState";
		internal const string VerticalStateName = "VerticalState";

		internal const string NotEditableStateName = "NotEditableState";
		internal const string EditableStateName = "EditableState";

	}
}
