using System;
using System.Collections.Generic;
using System.Text;




namespace Files.App.Controls.Primitives
{
	[TemplateVisualState( Name = HorizontalStateName , GroupName = OrientationStateGroupName )]
	[TemplateVisualState( Name = VerticalStateName , GroupName = OrientationStateGroupName )]

	[TemplateVisualState( Name = NotEditableStateName , GroupName = EditableStateGroupName )]
	[TemplateVisualState( Name = EditableStateName , GroupName = EditableStateGroupName )]

	[TemplateVisualState( Name = EnabledStateName , GroupName = EnabledStateGroupName )]
	[TemplateVisualState( Name = DisabledStateName , GroupName = EnabledStateGroupName )]




	public partial class PropertiesViewItem : Control
	{
		internal const string OrientationStateGroupName = "OrientationStateGroup";
		internal const string EditableStateGroupName = "EditableStateGroup";
		internal const string EnabledStateGroupName = "EnabledStateGroup";


		internal const string HorizontalStateName = "HorizontalState";
		internal const string VerticalStateName = "VerticalState";

		internal const string NotEditableStateName = "NotEditableState";
		internal const string EditableStateName = "EditableState";

		internal const string EnabledStateName = "Enabledtate";
		internal const string DisabledStateName = "DisabledState";

	}
}
