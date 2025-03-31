// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Files.App.Controls.Primitives
{
	public partial class RingShape : Path
	{
		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double StartAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 90.0d)]
		public partial double EndAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = SweepDirection.Clockwise)]
		public partial SweepDirection SweepDirection { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double MinAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 360.0d)]
		public partial double MaxAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double RadiusWidth { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double RadiusHeight { get; set; }

		[GeneratedDependencyProperty(DefaultValue = false)]
		public partial bool IsCircle { get; set; }

		[GeneratedDependencyProperty]
		public partial Point Center { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double ActualRadiusWidth { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double ActualRadiusHeight { get; set; }

		partial void OnStartAngleChanged(double newValue)
		{
			StartAngleChanged();
		}

		partial void OnEndAngleChanged(double newValue)
		{
			EndAngleChanged();
		}

		partial void OnSweepDirectionChanged(SweepDirection newValue)
		{
			SweepDirectionChanged();
		}

		partial void OnMinAngleChanged(double newValue)
		{
			MinMaxAngleChanged(false);
		}

		partial void OnMaxAngleChanged(double newValue)
		{
			MinMaxAngleChanged(true);
		}

		partial void OnRadiusWidthChanged(double newValue)
		{
			RadiusWidthChanged();
		}

		partial void OnRadiusHeightChanged(double newValue)
		{
			RadiusHeightChanged();
		}

		partial void OnIsCircleChanged(bool newValue)
		{
			IsCircleChanged();
		}
	}
}
