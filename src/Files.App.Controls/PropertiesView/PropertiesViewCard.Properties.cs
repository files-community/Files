// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommunityToolkit.WinUI;

namespace Files.App.Controls;

public partial class PropertiesViewCard
{
	[GeneratedDependencyProperty]
	public partial object? Header { get; set; }

	//[GeneratedDependencyProperty]
	//public partial IconElement? HeaderIcon { get; set; }

	[GeneratedDependencyProperty]
	public partial IconElement? ActionIcon { get; set; }

	[GeneratedDependencyProperty]
	public partial string? ActionIconToolTip { get; set; }

	[GeneratedDependencyProperty(DefaultValue = false)]
	public partial bool IsClickEnabled { get; set; }

	[GeneratedDependencyProperty(DefaultValue = true)]
	public partial bool IsActionIconVisible { get; set; }

	public GridLength LabelWidth
	{
		get => (GridLength)GetValue(LabelWidthProperty);
		set => SetValue(LabelWidthProperty, value);
	}

	public static readonly DependencyProperty LabelWidthProperty =
		DependencyProperty.Register(
			nameof(LabelWidth),
			typeof(GridLength),
			typeof(PropertiesViewCard),
			new PropertyMetadata(new GridLength(80)));

	//[GeneratedDependencyProperty(DefaultValue = PropertiesViewCardContentAlignment.Horizontal)]
	//public partial PropertiesViewCardContentAlignment ContentAlignment { get; set; }


	[GeneratedDependencyProperty(DefaultValue = Orientation.Horizontal)]
	public partial Orientation Orientation { get; set; }




	#region ContentAlignment for Breakpoints

	[GeneratedDependencyProperty( DefaultValue = HorizontalAlignment.Left )]
	public partial HorizontalAlignment LeftBreakpointHorizontalContentAlignment {  get; set; }

	[GeneratedDependencyProperty( DefaultValue = HorizontalAlignment.Right )]
	public partial HorizontalAlignment RightBreakpointHorizontalContentAlignment { get; set; }

	[GeneratedDependencyProperty( DefaultValue = HorizontalAlignment.Stretch )]
	public partial HorizontalAlignment WrappedBreakpointHorizontalContentAlignment { get; set; }



	[GeneratedDependencyProperty( DefaultValue = VerticalAlignment.Center )]
	public partial VerticalAlignment LeftBreakpointVerticalContentAlignment { get; set; }

	[GeneratedDependencyProperty( DefaultValue = VerticalAlignment.Center )]
	public partial VerticalAlignment RightBreakpointVerticalContentAlignment { get; set; }

	[GeneratedDependencyProperty( DefaultValue = VerticalAlignment.Center )]
	public partial VerticalAlignment WrappedBreakpointVerticalContentAlignment { get; set; }

	#endregion
}

public enum PropertiesViewCardContentAlignment
{
	Horizontal,
	Vertical
}
