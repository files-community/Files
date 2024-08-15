// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
	public partial class StorageRing
	{
		#region ValueRingThickness (double)

		/// <summary>
		/// The ValueRing Thickness property.
		/// </summary>
		public static readonly DependencyProperty ValueRingThicknessProperty =
			DependencyProperty.Register(
				nameof(ValueRingThickness),
				typeof(double),
				typeof(StorageRing),
				new PropertyMetadata(0.0, (d, e) => ((StorageRing)d).OnValueRingThicknessChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the Track ring Thickness.
		/// </summary>
		/// <value>
		/// The value ring thickness.
		/// </value>
		public double ValueRingThickness
		{
			get => (double)GetValue(ValueRingThicknessProperty);
			set => SetValue(ValueRingThicknessProperty, value);
		}

		private void OnValueRingThicknessChanged(double oldValue, double newValue)
		{
			UpdateRingThickness(this, newValue, false);
			UpdateRings(this);
		}

		#endregion

		#region TrackRingThickness (double)

		/// <summary>
		/// The TrackRing Thickness property.
		/// </summary>
		public static readonly DependencyProperty TrackRingThicknessProperty =
			DependencyProperty.Register(
				nameof(TrackRingThickness),
				typeof(double),
				typeof(StorageRing),
				new PropertyMetadata(0.0, (d, e) => ((StorageRing)d).OnTrackRingThicknessChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the Track ring Thickness.
		/// </summary>
		/// <value>
		/// The track ring thickness.
		/// </value>
		public double TrackRingThickness
		{
			get => (double)GetValue(TrackRingThicknessProperty);
			set => SetValue(TrackRingThicknessProperty, value);
		}

		private void OnTrackRingThicknessChanged(double oldValue, double newValue)
		{
			UpdateRingThickness(this, newValue, true);
			UpdateRings(this);
		}

		#endregion

		#region MinAngle (double)

		/// <summary>
		/// Identifies the MinAngle dependency property.
		/// </summary>
		public static readonly DependencyProperty MinAngleProperty =
		DependencyProperty.Register(
			nameof(MinAngle),
			typeof(double),
			typeof(StorageRing),
			new PropertyMetadata(0.0, (d, e) => ((StorageRing)d).OnMinAngleChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the MinAngle
		/// </summary>
		public double MinAngle
		{
			get => (double)GetValue(MinAngleProperty);
			set => SetValue(MinAngleProperty, value);
		}

		private void OnMinAngleChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, newValue, MaxAngle);
			UpdateRings(this);
		}

		#endregion

		#region MaxAngle (double)

		/// <summary>
		/// Identifies the MinAngle dependency property.
		/// </summary>
		public static readonly DependencyProperty MaxAngleProperty =
		DependencyProperty.Register(
			nameof(MaxAngle),
			typeof(double),
			typeof(StorageRing),
			new PropertyMetadata(360.0, (d, e) => ((StorageRing)d).OnMaxAngleChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the MaxAngle
		/// </summary>
		public double MaxAngle
		{
			get => (double)GetValue(MaxAngleProperty);
			set => SetValue(MaxAngleProperty, value);
		}

		private void OnMaxAngleChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
			UpdateRings(this);
		}

		#endregion

		#region StartAngle (double)

		/// <summary>
		/// Identifies the StartAngle dependency property.
		/// </summary>
		public static readonly DependencyProperty StartAngleProperty =
		DependencyProperty.Register(
			nameof(StartAngle),
			typeof(double),
			typeof(StorageRing),
			new PropertyMetadata(0.0, (d, e) => ((StorageRing)d).OnStartAngleChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the StartAngle
		/// </summary>
		public double StartAngle
		{
			get => (double)GetValue(StartAngleProperty);
			set => SetValue(StartAngleProperty, value);
		}

		private void OnStartAngleChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
			ValidateStartAngle(this, newValue);
			UpdateRings(this);
		}

		#endregion

		#region Percent (double)

		/// <summary>
		/// Identifies the Percent dependency property.
		/// </summary>
		public static readonly DependencyProperty PercentProperty =
			DependencyProperty.Register(
				nameof(Percent),
				typeof(double),
				typeof(StorageRing),
				new PropertyMetadata(null, (d, e) => ((StorageRing)d).OnPercentChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the current value as a Percentage between 0.0 and 100.0.
		/// </summary>
		public double Percent
		{
			get => (double)GetValue(PercentProperty);
			set => SetValue(PercentProperty, value);
		}

		private void OnPercentChanged(double oldValue, double newValue)
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

		#endregion

		#region PercentCaution (double)

		/// <summary>
		/// Identifies the PercentCaution dependency property
		/// </summary>
		public static readonly DependencyProperty PercentCautionProperty =
			DependencyProperty.Register(
				nameof(PercentCaution),
				typeof(double),
				typeof(StorageRing),
				new PropertyMetadata(75.01, (d, e) => ((StorageRing)d).OnPercentCautionChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the PercentCaution double value
		/// </summary>
		public double PercentCaution
		{
			get => (double)GetValue(PercentCautionProperty);
			set => SetValue(PercentCautionProperty, value);
		}

		private void OnPercentCautionChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		#endregion

		#region PercentCritical (double)

		/// <summary>
		/// Identifies the PercentCritical dependency property
		/// </summary>
		public static readonly DependencyProperty PercentCriticalProperty =
			DependencyProperty.Register(
				nameof(PercentCritical),
				typeof(double),
				typeof(StorageRing),
				new PropertyMetadata(90.01, (d, e) => ((StorageRing)d).OnPercentCriticalChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the PercentCritical double value
		/// </summary>
		public double PercentCritical
		{
			get => (double)GetValue(PercentCriticalProperty);
			set => SetValue(PercentCriticalProperty, value);
		}

		private void OnPercentCriticalChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		#endregion

		#region RangeBase Methods

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

		#endregion

		#region Protected ValueAngle (double)

		/// <summary>
		/// Identifies the ValueAngle dependency property.
		/// </summary>
		protected static readonly DependencyProperty ValueAngleProperty =
			DependencyProperty.Register(
				nameof(ValueAngle),
				typeof(double),
				typeof(StorageRing),
				new PropertyMetadata(null));

		/// <summary>
		/// Gets or sets the current angle of the Ring (between MinAngle and MaxAngle).
		/// </summary>
		protected double ValueAngle
		{
			get => (double)GetValue(ValueAngleProperty);
			set => SetValue(ValueAngleProperty, value);
		}

		#endregion

		#region Protected AdjustedSize (double)

		/// <summary>
		/// Identifies the AdjustedSize dependency property.
		/// </summary>
		protected static readonly DependencyProperty AdjustedSizeProperty =
			DependencyProperty.Register(
				nameof(AdjustedSize),
				typeof(double),
				typeof(StorageRing),
				new PropertyMetadata(16.0));

		/// <summary>
		/// Gets or sets the AdjustedSize of the control.
		/// </summary>
		protected double AdjustedSize
		{
			get => (double)GetValue(AdjustedSizeProperty);
			set => SetValue(AdjustedSizeProperty, value);
		}

		#endregion
	}
}
