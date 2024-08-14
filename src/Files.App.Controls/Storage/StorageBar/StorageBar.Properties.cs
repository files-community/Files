// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.Controls
{
	public partial class StorageBar : RangeBase
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
				new PropertyMetadata(4.0, OnValueBarHeightChanged));


		/// <summary>
		/// Gets or sets the height of the Value Bar.
		/// </summary>
		public double ValueBarHeight
		{
			get { return (double)GetValue( ValueBarHeightProperty ); }
			set { SetValue( ValueBarHeightProperty , value ); }
		}


		/// <summary>
		/// Handles the change in ValueBar Height property.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="e">The event arguments containing the old and new values.</param>
		private static void OnValueBarHeightChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			if ( e.OldValue != e.NewValue )
			{
				ValueBarHeightChanged( d , (double)e.NewValue );
			}
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
				new PropertyMetadata(2.0, OnTrackBarHeightChanged));


		/// <summary>
		/// Gets or sets the height of the Track Bar.
		/// </summary>
		public double TrackBarHeight
		{
			get { return (double)GetValue( TrackBarHeightProperty ); }
			set { SetValue( TrackBarHeightProperty , value ); }
		}


		/// <summary>
		/// Handles the change in TrackBar Height property.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="e">The event arguments containing the old and new values.</param>
		private static void OnTrackBarHeightChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			if ( e.OldValue != e.NewValue )
			{
				TrackBarHeightChanged( d , (double)e.NewValue );
			}
		}

		#endregion




		#region BarShape (enum BarShape)

		/// <summary>
		/// Identifies the BarShape dependency property.
		/// </summary>
		public static readonly DependencyProperty BarShapeProperty =
			DependencyProperty.Register(
				nameof(BarShape),
				typeof(BarShapes),
				typeof(StorageBar),
				new PropertyMetadata(BarShapes.Round, OnBarShapeChanged));


		/// <summary>
		/// Gets or sets an Enum value to choose from our two BarShapes. (Round, Flat)
		/// </summary>
		public BarShapes BarShape
		{
			get => (BarShapes)GetValue( BarShapeProperty );
			set => SetValue( BarShapeProperty , value );
		}


		/// <summary>
		/// Handles the change in BarShape property.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="e">The event arguments containing the old and new values.</param>
		private static void OnBarShapeChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			if ( e.OldValue != e.NewValue )
			{
				BarShapeChanged( d , (BarShapes)e.NewValue );
			}
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
				new PropertyMetadata(0.0, OnPercentChanged));


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
			StorageBar storageBar = (StorageBar)d;

			storageBar.DoubleToPercentage( storageBar.Value , storageBar.Minimum , storageBar.Maximum );
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
				new PropertyMetadata(75.1, OnPercentCautionChanged));


		/// <summary>
		/// Gets or sets the PercentCaution double value.
		/// </summary>
		public double PercentCaution
		{
			get { return (double)GetValue( PercentCautionProperty ); }
			set { SetValue( PercentCautionProperty , value ); }
		}


		/// <summary>
		/// Handles the change in the PercentCaution property.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="e">The event arguments containing the old and new values.</param>
		private static void OnPercentCautionChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			if ( e.OldValue != e.NewValue )
			{
				PercentCautionChanged( d , (double)e.NewValue );
			}
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
				new PropertyMetadata(89.9, OnPercentCriticalChanged));


		/// <summary>
		/// Gets or sets the PercentCritical double value.
		/// </summary>
		public double PercentCritical
		{
			get { return (double)GetValue( PercentCriticalProperty ); }
			set { SetValue( PercentCriticalProperty , value ); }
		}


		/// <summary>
		/// Handles the change in the PercentCritical property.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="e">The event arguments containing the old and new values.</param>
		private static void OnPercentCriticalChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			if ( e.OldValue != e.NewValue )
			{
				PercentCriticalChanged( d , (double)e.NewValue );
			}
		}

		#endregion




		#region Derived RangeBase Events

		/// <inheritdoc/>
		protected override void OnValueChanged(double oldValue , double newValue)
		{
			SetOldValue( oldValue );

			base.OnValueChanged( oldValue , newValue );

			OnValueChanged( this );
		}



		/// <inheritdoc/>
		protected override void OnMaximumChanged(double oldValue , double newValue)
		{
			base.OnMaximumChanged( oldValue , newValue );
			UpdateValue( this , oldValue , newValue, false, -1.0 );
		}



		/// <inheritdoc/>
		protected override void OnMinimumChanged(double oldValue , double newValue)
		{
			base.OnMinimumChanged( oldValue , newValue );
			UpdateValue( this , oldValue , newValue , false , -1.0 );
		}

		#endregion
	}
}
