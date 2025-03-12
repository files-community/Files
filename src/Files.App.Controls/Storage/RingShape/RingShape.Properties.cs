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
		public partial double EndAngle{get;set;}

		[GeneratedDependencyProperty(DefaultValue = SweepDirection.Clockwise)]
		public partial SweepDirection SweepDirection{get;set;}

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double MinAngle{get;set;}

		[GeneratedDependencyProperty(DefaultValue = 360.0d)]
		public partial double MaxAngle{get;set;}

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double RadiusWidth{get;set;}

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double RadiusHeight{get;set;}

		[GeneratedDependencyProperty]
		public partial bool IsCircle{get;set;}

		[GeneratedDependencyProperty]
		public partial Point Center{get;set;}

		[GeneratedDependencyProperty]
		public partial double ActualRadiusWidth{get;set;}

		[GeneratedDependencyProperty]
		public partial double ActualRadiusHeight{get;set;}

		protected virtual void OnStartAngleChanged(double oldValue, double newValue)
		{
			StartAngleChanged();
		}

		protected virtual void OnEndAngleChanged(double oldValue, double newValue)
		{
			EndAngleChanged();
		}

		protected virtual void OnSweepDirectionChanged(SweepDirection oldValue, SweepDirection newValue)
		{
			SweepDirectionChanged();
		}

		protected virtual void OnMinAngleChanged(double oldValue, double newValue)
		{
			MinMaxAngleChanged(false);
		}

		protected virtual void OnMaxAngleChanged(double oldValue, double newValue)
		{
			MinMaxAngleChanged(true);
		}

		protected virtual void OnRadiusWidthChanged(double oldValue, double newValue)
		{
			RadiusWidthChanged();
		}

		protected virtual void OnRadiusHeightChanged(double oldValue, double newValue)
		{
			RadiusHeightChanged();
		}

		protected virtual void OnIsCircleChanged(bool oldValue, bool newValue)
		{
			IsCircleChanged();
		}
	}
}
