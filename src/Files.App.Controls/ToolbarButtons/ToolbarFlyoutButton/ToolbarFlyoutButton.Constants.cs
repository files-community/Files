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

	public partial class ToolbarFlyoutButton : ToolbarButton
	{

	}
}