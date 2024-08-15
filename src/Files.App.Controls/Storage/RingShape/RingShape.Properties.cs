// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Files.App.Controls.Primitives
{
	public partial class RingShape : Path
	{
		#region StartAngle (double)

		/// <summary>
		/// The Start Angle property.
		/// </summary>
		public static readonly DependencyProperty StartAngleProperty =
			DependencyProperty.Register(
				nameof(StartAngle),
				typeof(double),
				typeof(RingShape),
				new PropertyMetadata(0.0, OnStartAngleChanged));

		/// <summary>
		/// Gets or sets the start angle.
		/// </summary>
		/// <value>
		/// The start angle.
		/// </value>
		public double StartAngle
		{
			get => (double)GetValue(StartAngleProperty);
			set => SetValue(StartAngleProperty, value);
		}

		/// <summary>
		/// Function invoked as the StartAngleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnStartAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.StartAngleChanged(d, (double)e.NewValue);
		}

		#endregion

		#region EndAngle (double)

		/// <summary>
		/// The End Angle property.
		/// </summary>
		public static readonly DependencyProperty EndAngleProperty =
			DependencyProperty.Register(
				nameof(EndAngle),
				typeof(double),
				typeof(RingShape),
				new PropertyMetadata(90.0, OnEndAngleChanged));

		/// <summary>
		/// Gets or sets the end angle.
		/// </summary>
		/// <value>
		/// The end angle.
		/// </value>
		public double EndAngle
		{
			get => (double)GetValue(EndAngleProperty);
			set => SetValue(EndAngleProperty, value);
		}

		/// <summary>
		/// Function invoked as the EndAngleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnEndAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.EndAngleChanged(d, (double)e.NewValue);
		}

		#endregion

		#region SweepDirection (SweepDirection)

		/// <summary>
		/// The SweepDirection property.
		/// </summary>
		public static readonly DependencyProperty SweepDirectionProperty =
			DependencyProperty.Register(
				nameof(SweepDirection),
				typeof(SweepDirection),
				typeof(RingShape),
				new PropertyMetadata(
					SweepDirection.Clockwise, OnSweepDirectionChanged));

		/// <summary>
		/// Gets or sets the SweepDirection.
		/// </summary>
		/// <value>
		/// The SweepDirection.
		/// </value>
		public SweepDirection SweepDirection
		{
			get => (SweepDirection)GetValue(SweepDirectionProperty);
			set => SetValue(SweepDirectionProperty, value);
		}

		/// <summary>
		/// Function invoked as the SweepDirectionProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnSweepDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.SweepDirectionChanged(d, (SweepDirection)e.NewValue);
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
			typeof(RingShape),
			new PropertyMetadata(0.0, OnMinAngleChanged));

		/// <summary>
		/// Gets or sets the MinAngle
		/// </summary>
		public double MinAngle
		{
			get => (double)GetValue(MinAngleProperty);
			set => SetValue(MinAngleProperty, value);
		}

		/// <summary>
		/// Function invoked as the MinAngleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnMinAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.MinMaxAngleChanged(d, (double)e.NewValue, false);
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
			typeof(RingShape),
			new PropertyMetadata(360.0, OnMaxAngleChanged));

		/// <summary>
		/// Gets or sets the MaxAngle
		/// </summary>
		public double MaxAngle
		{
			get => (double)GetValue(MaxAngleProperty);
			set => SetValue(MaxAngleProperty, value);
		}

		/// <summary>
		/// Function invoked as the MaxAngleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnMaxAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.MinMaxAngleChanged(ringShape, (double)e.NewValue, true);
		}

		#endregion

		#region RadiusWidth (double)

		/// <summary>
		/// Identifies the RadiusWidth dependency property.
		/// </summary>
		public static readonly DependencyProperty RadiusWidthProperty =
		DependencyProperty.Register(
			nameof(RadiusWidth),
			typeof(double),
			typeof(RingShape),
			new PropertyMetadata(0.0, OnRadiusWidthChanged));

		/// <summary>
		/// Gets or sets the Radius along the Width of the shape
		/// </summary>
		public double RadiusWidth
		{
			get => (double)GetValue(RadiusWidthProperty);
			set => SetValue(RadiusWidthProperty, value);
		}

		/// <summary>
		/// Function invoked as the RadiusWidthProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnRadiusWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.RadiusWidthChanged(d, (double)e.NewValue);
		}

		#endregion

		#region RadiusHeight (double)

		/// <summary>
		/// Identifies the RadiusHeight dependency property.
		/// </summary>
		public static readonly DependencyProperty RadiusHeightProperty =
		DependencyProperty.Register(
			nameof(RadiusHeight),
			typeof(double),
			typeof(RingShape),
			new PropertyMetadata(0.0, OnRadiusHeightChanged));

		/// <summary>
		/// Gets or sets the Radius along the Height of the shape
		/// </summary>
		public double RadiusHeight
		{
			get => (double)GetValue(RadiusHeightProperty);
			set => SetValue(RadiusHeightProperty, value);
		}

		/// <summary>
		/// Function invoked as the RadiusHeightProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnRadiusHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.RadiusHeightChanged(d, (double)e.NewValue);
		}
		#endregion

		#region IsCircle (bool)

		/// <summary>
		/// Identifies the <see cref="IsCircle"/> property.
		/// </summary>
		public static readonly DependencyProperty IsCircleProperty =
		DependencyProperty.Register(
			nameof(IsCircle),
			typeof(bool),
			typeof(RingShape),
			new PropertyMetadata(false, OnIsCircleChanged));

		/// <summary>
		/// Gets or sets a value indicating whether the shape should be constrained as a Circle.
		/// </summary>
		public bool IsCircle
		{
			get => (bool)GetValue(IsCircleProperty);
			set => SetValue(IsCircleProperty, value);
		}

		/// <summary>
		/// Function invoked as the IsCircleProperty is changed
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private static void OnIsCircleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			RingShape ringShape = (RingShape)d;

			ringShape.IsCircleChanged(d, (bool)e.NewValue);
		}

		#endregion

		#region Protected Centre (Point)

		/// <summary>
		/// Identifies the Protected Center dependency property.
		/// </summary>
		protected static readonly DependencyProperty CenterProperty =
			DependencyProperty.Register(
				nameof(Center),
				typeof(Point),
				typeof(RingShape),
				new PropertyMetadata(null));

		/// <summary>
		/// Gets or sets the Center point
		/// </summary>
		protected Point Center
		{
			get => (Point)GetValue(CenterProperty);
			set => SetValue(CenterProperty, value);
		}

		#endregion

		#region Protected ActualRadiusWidth (double)

		/// <summary>
		/// Identifies the Protected ActualRadiusWidth dependency property.
		/// </summary>
		protected static readonly DependencyProperty ActualRadiusWidthProperty =
			DependencyProperty.Register(
				nameof(ActualRadiusWidth),
				typeof(double),
				typeof(RingShape),
				new PropertyMetadata(null));

		/// <summary>
		/// Gets or sets the ActualRadiusWidth double value
		/// </summary>
		protected double ActualRadiusWidth
		{
			get => (double)GetValue(ActualRadiusWidthProperty);
			set => SetValue(ActualRadiusWidthProperty, value);
		}

		#endregion

		#region Protected ActualRadiusHeight (double)

		/// <summary>
		/// Identifies the Protected ActualRadiusHeight dependency property.
		/// </summary>
		protected static readonly DependencyProperty ActualRadiusHeightProperty =
			DependencyProperty.Register(
				nameof(ActualRadiusHeight),
				typeof(double),
				typeof(RingShape),
				new PropertyMetadata(null));

		/// <summary>
		/// Gets or sets the ActualRadiusHeight double value
		/// </summary>
		protected double ActualRadiusHeight
		{
			get => (double)GetValue(ActualRadiusHeightProperty);
			set => SetValue(ActualRadiusHeightProperty, value);
		}

	#endregion
	}
}
