// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.Controls
{
	public partial class StorageRing : RangeBase
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
				new PropertyMetadata(0.0, OnValueRingThicknessChanged));



		/// <summary>
		/// Gets or sets the Track ring Thickness.
		/// </summary>
		/// <value>
		/// The value ring thickness.
		/// </value>
		public double ValueRingThickness
		{
			get { return (double)GetValue( ValueRingThicknessProperty ); }
			set { SetValue( ValueRingThicknessProperty , value ); }
		}



		/// <summary>
		/// Function invoked as the ValueRingThicknessProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnValueRingThicknessChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.ValueRingThicknessChanged( d , (double)e.NewValue );
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
				new PropertyMetadata(0.0, OnTrackRingThicknessChanged));



		/// <summary>
		/// Gets or sets the Track ring Thickness.
		/// </summary>
		/// <value>
		/// The track ring thickness.
		/// </value>
		public double TrackRingThickness
		{
			get { return (double)GetValue( TrackRingThicknessProperty ); }
			set { SetValue( TrackRingThicknessProperty , value ); }
		}



		/// <summary>
		/// Function invoked as the TrackRingThicknessProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnTrackRingThicknessChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.TrackRingThicknessChanged( d , (double)e.NewValue );
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
			new PropertyMetadata(0.0, OnMinAngleChanged));



		/// <summary>
		/// Gets or sets the MinAngle
		/// </summary>
		public double MinAngle
		{
			get { return (double)GetValue( MinAngleProperty ); }
			set { SetValue( MinAngleProperty , value ); }
		}



		/// <summary>
		/// Function invoked as the MinAngleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnMinAngleChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.MinAngleChanged( d , (double)e.NewValue );
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
			new PropertyMetadata(360.0, OnMaxAngleChanged));



		/// <summary>
		/// Gets or sets the MaxAngle
		/// </summary>
		public double MaxAngle
		{
			get { return (double)GetValue( MaxAngleProperty ); }
			set { SetValue( MaxAngleProperty , value ); }
		}



		/// <summary>
		/// Function invoked as the MaxAngleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnMaxAngleChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.MaxAngleChanged( d , (double)e.NewValue );
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
			new PropertyMetadata(0.0, OnStartAngleChanged));



		/// <summary>
		/// Gets or sets the StartAngle
		/// </summary>
		public double StartAngle
		{
			get { return (double)GetValue( StartAngleProperty ); }
			set { SetValue( StartAngleProperty , value ); }
		}



		/// <summary>
		/// Function invoked as the StartAngleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnStartAngleChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.StartAngleChanged( d , (double)e.NewValue );
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
				new PropertyMetadata(null, OnPercentChanged));


		/// <summary>
		/// Gets or sets the current value as a Percentage between 0.0 and 100.0.
		/// </summary>
		public double Percent
		{
			get { return (double)GetValue( PercentProperty ); }
			set { SetValue( PercentProperty , value ); }
		}


		/// <summary>
		/// Handles the change in the Percent property, and ensures the range is between 0.0 and 100.0.
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnPercentChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.DoubleToPercentage( storageRing.Value , storageRing.Minimum , storageRing.Maximum );
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
				new PropertyMetadata(75.01, OnPercentCautionChanged));


		/// <summary>
		/// Gets or sets the PercentCaution double value
		/// </summary>
		public double PercentCaution
		{
			get { return (double)GetValue( PercentCautionProperty ); }
			set { SetValue( PercentCautionProperty , value ); }
		}


		/// <summary>
		/// Handles the change in the PercentCaution property
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnPercentCautionChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.PercentCautionChanged( d , (double)e.NewValue );
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
				new PropertyMetadata(90.01, OnPercentCriticalChanged));


		/// <summary>
		/// Gets or sets the PercentCritical double value
		/// </summary>
		public double PercentCritical
		{
			get { return (double)GetValue( PercentCriticalProperty ); }
			set { SetValue( PercentCriticalProperty , value ); }
		}


		/// <summary>
		/// Handles the change in the PercentCritical property
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnPercentCriticalChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			StorageRing storageRing = (StorageRing)d;

			storageRing.PercentCriticalChanged( d , (double)e.NewValue );
		}

		#endregion




		#region RangeBase Methods

		/// <inheritdoc/>
		protected override void OnValueChanged(double oldValue , double newValue)
		{
			base.OnValueChanged(oldValue , newValue );

			StorageRing_ValueChanged( this , newValue , oldValue);
		}




		/// <inheritdoc/>
		protected override void OnMinimumChanged(double oldMinimum , double newMinimum)
		{
			base.OnMinimumChanged( oldMinimum , newMinimum );

			StorageRing_MinimumChanged( this , newMinimum );
		}




		/// <inheritdoc/>
		protected override void OnMaximumChanged(double oldMaximum , double newMaximum)
		{
			base.OnMaximumChanged( oldMaximum , newMaximum );

			StorageRing_MaximumChanged( this , newMaximum );
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
			get { return (double)GetValue( ValueAngleProperty ); }
			set { SetValue( ValueAngleProperty , value ); }
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
			get { return (double)GetValue( AdjustedSizeProperty ); }
			set { SetValue( AdjustedSizeProperty , value ); }
		}

		#endregion
	}
}
