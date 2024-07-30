// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	// Template Parts
	[TemplatePart( Name = ButtonThemedIconPartName , Type = typeof( ThemedIcon ) )]

	// Visual States
	[TemplateVisualState( Name = NormalStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = PointerOverStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = PressedStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = DisabledStateName , GroupName = CommonStatesGroupName )]

	[TemplateVisualState( Name = CheckedStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = CheckedPointerOverStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = CheckedPressedStateName , GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = CheckedDisabledStateName , GroupName = CommonStatesGroupName )]

	public partial class ToolbarToggleButton : ToolbarButton
	{

		internal const string CheckedStateName = "Checked";
		internal const string CheckedPointerOverStateName = "CheckedPointerOver";
		internal const string CheckedPressedStateName = "CheckedPressed";
		internal const string CheckedDisabledStateName = "CheckedDisabled";
	}
}
