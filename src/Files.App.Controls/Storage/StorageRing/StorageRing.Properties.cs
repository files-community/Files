// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class StorageRing
	{
		[GeneratedDependencyProperty(DefaultValue = 0d)]
		public partial double ValueRingThickness { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0d)]
		public partial double TrackRingThickness { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double MinAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 360.0d)]
		public partial double MaxAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double StartAngle { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
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
			UpdateRingThickness(newValue, false);
			UpdateRings();
		}

		partial void OnTrackRingThicknessChanged(double newValue)
		{
			UpdateRingThickness(newValue, true);
			UpdateRings();
		}

		partial void OnMinAngleChanged(double newValue)
		{
			UpdateValues(Value, _oldValue, false, -1.0);
			UpdateNormalizedAngles(newValue, MaxAngle);
			UpdateRings();
		}

		partial void OnMaxAngleChanged(double newValue)
		{
			UpdateValues(Value, _oldValue, false, -1.0);
			UpdateNormalizedAngles(MinAngle, newValue);
			UpdateRings();
		}

		partial void OnStartAngleChanged(double newValue)
		{
			UpdateValues(Value, _oldValue, false, -1.0);
			UpdateNormalizedAngles(MinAngle, newValue);
			ValidateStartAngle(newValue);
			UpdateRings();
		}

		partial void OnPercentChanged(double newValue)
		{
			return; //Read-only

			//Helpers.DoubleToPercentage(Value, Minimum, Maximum);

			//double adjustedPercentage;

			//if (newValue <= 0.0)
			//	adjustedPercentage = 0.0;
			//else if (newValue <= 100.0)
			//	adjustedPercentage = 100.0;
			//else
			//	adjustedPercentage = newValue;

			//UpdateValues(Value, _oldValue, true, adjustedPercentage);
			//UpdateVisualState();
			//UpdateRings();
		}

		partial void OnPercentCautionChanged(double newValue)
		{
			UpdateValues(Value, _oldValue, false, -1.0);
			UpdateVisualState();
			UpdateRings();
		}

		partial void OnPercentCriticalChanged(double newValue)
		{
			UpdateValues(Value, _oldValue, false, -1.0);
			UpdateVisualState();
			UpdateRings();
		}

		/// <inheritdoc/>
		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);

			UpdateValues(newValue, oldValue, false, -1.0);
			UpdateRings();
		}

		/// <inheritdoc/>
		protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
		{
			base.OnMinimumChanged(oldMinimum, newMinimum);

			UpdateRings();
		}

		/// <inheritdoc/>
		protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
		{
			base.OnMaximumChanged(oldMaximum, newMaximum);

			UpdateRings();
		}
	}
}
