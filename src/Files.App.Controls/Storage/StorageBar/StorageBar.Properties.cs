// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
	public partial class StorageBar
	{
		[GeneratedDependencyProperty(DefaultValue = 4.0d)]
		public partial double ValueBarHeight { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 2.0d)]
		public partial double TrackBarHeight { get; set; }

		[GeneratedDependencyProperty(DefaultValue = BarShapes.Round)]
		public partial BarShapes BarShape { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 0.0d)]
		public partial double Percent { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 75.1d)]
		public partial double PercentCaution { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 89.9d)]
		public partial double PercentCritical { get; set; }

		partial void OnValueBarHeightChanged(double newValue)
		{
			UpdateControl(this);
		}

		partial void OnTrackBarHeightChanged(double newValue)
		{
			UpdateControl(this);
		}

		partial void OnBarShapeChanged(BarShapes newValue)
		{
			UpdateControl(this);
		}

		partial void OnPercentChanged(double newValue)
		{
			return; //Read-only

			DoubleToPercentage(Value, Minimum, Maximum);
			UpdateControl(this);
		}

		partial void OnPercentCautionChanged(double newValue)
		{
			UpdateControl(this);
		}

		partial void OnPercentCriticalChanged(double newValue)
		{
			UpdateControl(this);
		}

		/// <inheritdoc/>
		protected override void OnValueChanged(double oldValue, double newValue)
		{
			_oldValue = oldValue;
			base.OnValueChanged(oldValue, newValue);
			UpdateValue(this, Value, _oldValue, false, -1.0);
		}

		/// <inheritdoc/>
		protected override void OnMaximumChanged(double oldValue, double newValue)
		{
			base.OnMaximumChanged(oldValue, newValue);
			UpdateValue(this, oldValue, newValue, false, -1.0);
		}

		/// <inheritdoc/>
		protected override void OnMinimumChanged(double oldValue, double newValue)
		{
			base.OnMinimumChanged(oldValue, newValue);
			UpdateValue(this, oldValue, newValue, false, -1.0);
		}
	}
}
