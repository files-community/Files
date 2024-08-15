// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
	public partial class StorageBar
	{
		#region Value Bar Height (double)

		/// <summary>
		/// Identifies the ValueBarHeight dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueBarHeightProperty =
			DependencyProperty.Register(
				nameof(ValueBarHeight),
				typeof(double),
				typeof(StorageBar),
				new PropertyMetadata(4.0, (d, e) => ((StorageBar)d).OnValueBarHeightChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the height of the Value Bar.
		/// </summary>
		public double ValueBarHeight
		{
			get => (double)GetValue(ValueBarHeightProperty);
			set => SetValue(ValueBarHeightProperty, value);
		}

		private void OnValueBarHeightChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		#endregion

		#region Track Bar Height (double)

		/// <summary>
		/// Identifies the TrackBarHeight dependency property.
		/// </summary>
		public static readonly DependencyProperty TrackBarHeightProperty =
			DependencyProperty.Register(
				nameof(TrackBarHeight),
				typeof(double),
				typeof(StorageBar),
				new PropertyMetadata(2.0, (d, e) => ((StorageBar)d).OnTrackBarHeightChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the height of the Track Bar.
		/// </summary>
		public double TrackBarHeight
		{
			get => (double)GetValue(TrackBarHeightProperty);
			set => SetValue(TrackBarHeightProperty, value);
		}

		private void OnTrackBarHeightChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		#endregion

		#region BarShape (BarShapes)

		/// <summary>
		/// Identifies the BarShape dependency property.
		/// </summary>
		public static readonly DependencyProperty BarShapeProperty =
			DependencyProperty.Register(
				nameof(BarShape),
				typeof(BarShapes),
				typeof(StorageBar),
				new PropertyMetadata(BarShapes.Round, (d, e) => ((StorageBar)d).OnBarShapeChanged((BarShapes)e.OldValue, (BarShapes)e.NewValue)));

		/// <summary>
		/// Gets or sets an Enum value to choose from our two BarShapes. (Round, Flat)
		/// </summary>
		public BarShapes BarShape
		{
			get => (BarShapes)GetValue(BarShapeProperty);
			set => SetValue(BarShapeProperty, value);
		}

		private void OnBarShapeChanged(BarShapes oldValue, BarShapes newValue)
		{
			UpdateControl(this);
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
				typeof(StorageBar),
				new PropertyMetadata(0.0, (d, e) => ((StorageBar)d).OnPercentChanged((double)e.OldValue, (double)e.NewValue)));

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

			UpdateControl(this);
		}

		#endregion

		#region PercentWarning (double)

		/// <summary>
		/// Identifies the PercentCaution dependency property.
		/// </summary>
		public static readonly DependencyProperty PercentCautionProperty =
			DependencyProperty.Register(
				nameof(PercentCaution),
				typeof(double),
				typeof(StorageBar),
				new PropertyMetadata(75.1, (d, e) => ((StorageBar)d).OnPercentCautionChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the PercentCaution double value.
		/// </summary>
		public double PercentCaution
		{
			get => (double)GetValue(PercentCautionProperty);
			set => SetValue(PercentCautionProperty, value);
		}

		private void OnPercentCautionChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		#endregion

		#region PercentCritical (double)

		/// <summary>
		/// Identifies the PercentCritical dependency property.
		/// </summary>
		public static readonly DependencyProperty PercentCriticalProperty =
			DependencyProperty.Register(
				nameof(PercentCritical),
				typeof(double),
				typeof(StorageBar),
				new PropertyMetadata(89.9, (d, e) => ((StorageBar)d).OnPercentCriticalChanged((double)e.OldValue, (double)e.NewValue)));

		/// <summary>
		/// Gets or sets the PercentCritical double value.
		/// </summary>
		public double PercentCritical
		{
			get => (double)GetValue(PercentCriticalProperty);
			set => SetValue(PercentCriticalProperty, value);
		}

		private void OnPercentCriticalChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		#endregion

		#region Derived RangeBase Events

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

		#endregion
	}
}
