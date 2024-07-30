// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.Controls
{
	// Template Parts
	[TemplatePart( Name = ButtonThemedIconPartName , Type = typeof( ThemedIcon ) )]

	// Visual States
	[TemplateVisualState( Name = NormalStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = PointerOverStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = PressedStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = DisabledStateName , GroupName = CommonStatesGroupName )]

	public partial class ToolbarButton : ButtonBase
	{
		// Template Part Names
		internal const string ButtonThemedIconPartName = "PART_ThemedIcon";

		// Visual State Group Names
		internal const string CommonStatesGroupName = "CommonStates";

		// CommonStates Visual State Names
		internal const string NormalStateName = "Normal";
		internal const string PointerOverStateName = "PointerOver";
		internal const string PressedStateName = "Pressed";
		internal const string DisabledStateName = "Disabled";
	}
}