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
			new PropertyMetadata(new GridLength(160)));

	[GeneratedDependencyProperty(DefaultValue = PropertiesViewCardContentAlignment.Right)]
	public partial PropertiesViewCardContentAlignment ContentAlignment { get; set; }
}

public enum PropertiesViewCardContentAlignment
{
	Right,
	Left,
	Vertical,
}
