// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	// Template Parts
	[TemplatePart(Name = FilledPathIconViewBox, Type = typeof(Viewbox))]
	[TemplatePart(Name = OutlinePathIconViewBox, Type = typeof(Viewbox))]
	[TemplatePart(Name = LayeredPathIconViewBox, Type = typeof(Viewbox))]
	[TemplatePart(Name = LayeredPathCanvas, Type = typeof(Canvas))]

	// Icon Type Visual States
	[TemplateVisualState(Name = OutlineTypeStateName, GroupName = IconTypeStateGroupName)]
	[TemplateVisualState(Name = FilledTypeStateName, GroupName = IconTypeStateGroupName)]
	[TemplateVisualState(Name = LayeredTypeStateName, GroupName = IconTypeStateGroupName)]

	// Icon Color Visual States
	[TemplateVisualState(Name = NormalStateName, GroupName = IconColorStateGroupName)]
	[TemplateVisualState(Name = CriticalStateName, GroupName = IconColorStateGroupName)]
	[TemplateVisualState(Name = CautionStateName, GroupName = IconColorStateGroupName)]
	[TemplateVisualState(Name = SuccessStateName, GroupName = IconColorStateGroupName)]
	[TemplateVisualState(Name = NeutralStateName, GroupName = IconColorStateGroupName)]
	[TemplateVisualState(Name = ToggleStateName, GroupName = IconColorStateGroupName)]
	[TemplateVisualState(Name = DisabledStateName, GroupName = IconColorStateGroupName)]
	[TemplateVisualState(Name = HighContrastStateName, GroupName = IconColorStateGroupName)]

	// Icon IsEnabled Visual States
	[TemplateVisualState(Name = EnabledStateName, GroupName = EnabledStateGroupName)]
	[TemplateVisualState(Name = NotEnabledStateName, GroupName = EnabledStateGroupName)]
	public sealed partial class ThemedIcon
	{
		// Visual State Group Names
		internal const string IconTypeStateGroupName = "IconTypeStates";
		internal const string IconColorStateGroupName = "IconColorStates";
		internal const string EnabledStateGroupName = "EnabledStates";

		// "Icon Type" Visual State Names
		internal const string OutlineTypeStateName = "Outline";
		internal const string FilledTypeStateName = "Filled";
		internal const string LayeredTypeStateName = "Layered";

		// "Icon State" Visual State Names
		internal const string NormalStateName = "Normal";
		internal const string CriticalStateName = "Critical";
		internal const string CautionStateName = "Caution";
		internal const string SuccessStateName = "Success";
		internal const string NeutralStateName = "Neutral";
		internal const string ToggleStateName = "Toggle";
		internal const string DisabledStateName = "Disabled";
		internal const string HighContrastStateName = "HighContrast";

		// "Enabled" Visual State Names
		internal const string EnabledStateName = "Enabled";
		internal const string NotEnabledStateName = "NotEnabled";

		// ViewBox Controls
		internal const string FilledPathIconViewBox = "PART_FilledIconViewBox";
		internal const string OutlinePathIconViewBox = "PART_OutlineIconViewBox";
		internal const string LayeredPathIconViewBox = "PART_LayeredIconViewBox";
		internal const string LayeredPathCanvas = "PART_LayerCanvas";

		// Path Controls
		internal const string FilledIconPath = "PART_FilledPath";
		internal const string OutlineIconPath = "PART_OutlinePath";
	}
}
