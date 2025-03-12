// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
	public partial class StorageRing
	{
		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double ValueRingThickness { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double TrackRingThickness { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double MinAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 360.0d)]
		public partial double MaxAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double StartAngle { get; set; }

		[GeneratedDependencyProperty]
		public partial double Percent { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 75.01d)]
		public partial double PercentCaution { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 90.01d)]
		public partial double PercentCritical { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double ValueAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 16.0d)]
		public partial double AdjustedSize { get; set; }

		partial void OnValueRingThicknessChanged(double newValue)
		{
			UpdateRingThickness(this, newValue, false);
			UpdateRings(this);
		}

		partial void OnTrackRingThicknessChanged(double newValue)
		{
			UpdateRingThickness(this, newValue, true);
			UpdateRings(this);
		}

		partial void OnMinAngleChanged(double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, newValue, MaxAngle);
			UpdateRings(this);
		}

		partial void OnMaxAngleChanged(double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
			UpdateRings(this);
		}

		partial void OnStartAngleChanged(double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
			ValidateStartAngle(this, newValue);
			UpdateRings(this);
		}

		partial void OnPercentChanged(double newValue)
		{
			return; //Read-only

			DoubleToPercentage(Value, Minimum, Maximum);

			double adjustedPercentage;

			if (newValue <= 0.0)
				adjustedPercentage = 0.0;
			else if (newValue <= 100.0)
				adjustedPercentage = 100.0;
			else
				adjustedPercentage = newValue;

			UpdateValues(this, Value, _oldValue, true, adjustedPercentage);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		partial void OnPercentCautionChanged(double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		partial void OnPercentCriticalChanged(double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		/// <inheritdoc/>
		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);
			StorageRing_ValueChanged(this, newValue, oldValue);
		}

		/// <inheritdoc/>
		protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
		{
			base.OnMinimumChanged(oldMinimum, newMinimum);
			StorageRing_MinimumChanged(this, newMinimum);
		}

		/// <inheritdoc/>
		protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
		{
			base.OnMaximumChanged(oldMaximum, newMaximum);
			StorageRing_MaximumChanged(this, newMaximum);
		}
	}
}
