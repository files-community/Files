// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

namespace Files.App.Controls
{
	/// <summary>
	/// StorageBar - Takes a set of values, converts them to a percentage
	/// and displays it across a Bar.
	/// </summary>
	public partial class StorageBar : RangeBase
	{
		#region 1. Private variables

		double                  _oldValue;              // Stores the previous value

		double                  _valueBarHeight;        // The stored Value Bar Height
		double                  _trackBarHeight;        // The stored Track Bar Height

		double                  _valueBarMaxWidth;      // The maximum width for the Value Bar
		double                  _trackBarMaxWidth;      // The maximum width for the Track Bar

		Grid?                   _containerGrid;         // Reference to the container Grid
		Size?                   _containerSize;         // Reference to the container Size

		ColumnDefinition?       _valueColumn;           // Reference to the ValueBar Column
		ColumnDefinition?       _trackColumn;           // Reference to the TrackBar Column
		ColumnDefinition?       _gapColumn;             // Reference to the Gap Column

		Border?                 _valueBarBorder;        // Reference to the Value Bar Border
		Border?                 _trackBarBorder;        // Reference to the Track Bar Border

		BarShapes               _barShape;				// Reference to the BarShape

		double                  _gapWidth;              // Stores the Gap between Value and Track Bars
		double                  _smallerHeight;         // Stores the smaller between Value and Track Bars

		#endregion




		#region 2. Private variable setters

		/// <summary>
		/// Sets the private old Value
		/// </summary>
		void SetOldValue(double value)
		{
			_oldValue = value;
		}


		/// <summary>
		/// Sets the private ValueBar Height value
		/// </summary>
		void SetValueBarHeight(double value)
		{
			_valueBarHeight = value;
		}


		/// <summary>
		/// Sets the private TrackBar Height value
		/// </summary>
		void SetTrackBarHeight(double value)
		{
			_trackBarHeight = value;
		}


		/// <summary>
		/// Sets the private ValueBar maximum width value
		/// </summary>
		void SetValueBarMaxWidth(double value)
		{
			_valueBarMaxWidth = value;
		}


		/// <summary>
		/// Sets the private TrackBar maximum width value
		/// </summary>
		void SetTrackBarMaxWidth(double value)
		{
			_trackBarMaxWidth = value;
		}


		/// <summary>
		/// Sets the private Container Grid reference
		/// </summary>
		void SetContainerGrid(Grid grid)
		{
			_containerGrid = grid;
		}


		/// <summary>
		/// Sets the private Container Size
		/// </summary>
		void SetContainerSize(Size size)
		{
			_containerSize = size;
		}


		/// <summary>
		/// Sets the private Value ColumnDefinition reference
		/// </summary>
		void SetValueColumn(ColumnDefinition columnDefinition)
		{
			_valueColumn = columnDefinition;
		}


		/// <summary>
		/// Sets the private Track ColumnDefinition reference
		/// </summary>
		void SetTrackColumn(ColumnDefinition columnDefinition)
		{
			_trackColumn = columnDefinition;
		}


		/// <summary>
		/// Sets the private Gap ColumnDefinition reference
		/// </summary>
		void SetGapColumn(ColumnDefinition columnDefinition)
		{
			_gapColumn = columnDefinition;
		}


		/// <summary>
		/// Sets the private ValueBar Border reference
		/// </summary>
		void SetValueBarBorder(Border valueBarBorder)
		{
			_valueBarBorder = valueBarBorder;
		}


		/// <summary>
		/// Sets the private TrackBar Border reference
		/// </summary>
		void SetTrackBarBorder(Border trackBarBorder)
		{
			_trackBarBorder = trackBarBorder;
		}


		/// <summary>
		/// Sets the private BarShape reference
		/// </summary>
		void SetBarShape(BarShapes barShape)
		{
			_barShape = barShape;
		}


		/// <summary>
		/// Sets the private Gap Width value
		/// </summary>
		void SetGapWidth(double value)
		{
			_gapWidth = value;
		}


		/// <summary>
		/// Sets the private Smaller Height value
		/// </summary>
		void SetSmallerHeight(double value)
		{
			_smallerHeight = value;
		}

		#endregion




		#region 3. Private variable getters

		/// <summary>
		/// Gets the old Value
		/// </summary>
		double GetOldValue()
		{
			return _oldValue;
		}


		/// <summary>
		/// Gets the ValueBar Height
		/// </summary>
		double GetValueBarHeight()
		{
			return _valueBarHeight;
		}


		/// <summary>
		/// Gets the TrackBar Height
		/// </summary>
		double GetTrackBarHeight()
		{
			return _trackBarHeight;
		}


		/// <summary>
		/// Gets the ValueBar max width
		/// </summary>
		double GetValueBarMaxWidth()
		{
			return _valueBarMaxWidth;
		}


		/// <summary>
		/// Gets the TrackBar max width
		/// </summary>
		double GetTrackBarMaxWidth()
		{
			return _trackBarMaxWidth;
		}


		/// <summary>
		/// Gets the Container Grid reference
		/// </summary>
		Grid? GetContainerGrid()
		{
			return _containerGrid;
		}


		/// <summary>
		/// Gets the Container Size
		/// </summary>
		Size? GetContainerSize()
		{
			return _containerSize;
		}


		/// <summary>
		/// Gets the Value ColumnDefinition reference
		/// </summary>
		ColumnDefinition? GetValueColumn()
		{
			return _valueColumn;
		}


		/// <summary>
		/// Gets the Track ColumnDefinition reference
		/// </summary>
		ColumnDefinition? GetTrackColumn()
		{
			return _trackColumn;
		}


		/// <summary>
		/// Gets the Gap ColumnDefinition reference
		/// </summary>
		ColumnDefinition? GetGapColumn()
		{
			return _gapColumn;
		}


		/// <summary>
		/// Gets the ValueBar Border reference
		/// </summary>
		Border? GetValueBarBorder()
		{
			return _valueBarBorder;
		}


		/// <summary>
		/// Gets the TrackBar Border reference
		/// </summary>
		Border? GetTrackBarBorder()
		{
			return _trackBarBorder;
		}


		/// <summary>
		/// Gets the BarShape reference
		/// </summary>
		BarShapes GetBarShape()
		{
			return _barShape;
		}


		/// <summary>
		/// Gets the Gap Width
		/// </summary>
		double GetGapWidth()
		{
			return _gapWidth;
		}


		/// <summary>
		/// Gets the Smaller Height
		/// </summary>
		double GetSmallerHeight()
		{
			return _smallerHeight;
		}

		#endregion




		#region 4. Initialisation

		/// <summary>
		/// Applies an implicit Style of a matching TargetType
		/// </summary>
		public StorageBar()
		{
			SizeChanged -= StorageBar_SizeChanged;
			Unloaded -= StorageBar_Unloaded;
			IsEnabledChanged -= StorageBar_IsEnabledChanged;

			this.DefaultStyleKey = typeof( StorageBar );

			SizeChanged += StorageBar_SizeChanged;
			Unloaded += StorageBar_Unloaded;
			IsEnabledChanged += StorageBar_IsEnabledChanged;
		}



		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateInitialLayout( this );
		}

		#endregion




		#region 5. Handle Property Changes

		/// <summary>
		/// Handles the Value and Track Bar Brushes Changed events
		/// </summary>
		/// <param name="d"></param>
		/// <param name="newBrush"></param>
		private static void BarBrushChanged(DependencyObject d , Brush newBrush)
		{
			var storageBar = d as StorageBar;

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the Value Bar Height's double value Changed event
		/// </summary>
		/// <param name="d"></param>
		/// <param name="newHeight"></param>
		private static void ValueBarHeightChanged(DependencyObject d , double newHeight)
		{
			var storageBar = d as StorageBar;

			storageBar.SetValueBarHeight( newHeight );

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the Track Bar Height's double value Changed event
		/// </summary>
		/// <param name="d"></param>
		/// <param name="newHeight"></param>
		private static void TrackBarHeightChanged(DependencyObject d , double newHeight)
		{
			var storageBar = d as StorageBar;

			storageBar.SetTrackBarHeight( newHeight );

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the BarShape's enum value Changed event
		/// </summary>
		/// <param name="d"></param>
		/// <param name="newBarShape">The new BarShape</param>
		private static void BarShapeChanged(DependencyObject d , BarShapes newBarShape)
		{
			var storageBar = d as StorageBar;

			storageBar.SetBarShape( newBarShape );

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the Precent double value Changed event
		/// </summary>
		/// <param name="d"></param>
		/// <param name="newPercent">The new Percent value</param>
		private static void PercentChanged(DependencyObject d , double newPercent)
		{
			var storageBar = d as StorageBar;

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the PrecentCaution double value Changed event
		/// </summary>
		/// <param name="d"></param>
		/// <param name="newPercentValue">The new PercentCaution value</param>
		private static void PercentCautionChanged(DependencyObject d , double newPercentCautionValue)
		{
			var storageBar = d as StorageBar;

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the PrecentCritical double value Changed event
		/// </summary>
		/// <param name="d"></param>
		/// <param name="newPercentValue">The new PercentCritical value</param>
		private static void PercentCriticalChanged(DependencyObject d , double newPercentCriticalValue)
		{
			var storageBar = d as StorageBar;

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the IsEnabledChanged event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StorageBar_IsEnabledChanged(object sender , DependencyPropertyChangedEventArgs e)
		{
			var storageBar = sender as StorageBar;

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the Unloaded event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StorageBar_Unloaded(object sender , RoutedEventArgs e)
		{
			var storageBar = sender as StorageBar;

			SizeChanged -= StorageBar_SizeChanged;
			Unloaded -= StorageBar_Unloaded;
			IsEnabledChanged -= StorageBar_IsEnabledChanged;
		}




		/// <summary>
		/// Handles the SizeChanged event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StorageBar_SizeChanged(object sender , SizeChangedEventArgs e)
		{
			var storageBar = sender as StorageBar;

			UpdateContainer( storageBar , (Size)e.NewSize );

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Handles the RangeBase's ValueChanged event
		/// </summary>
		/// <param name="d"></param>
		private void OnValueChanged(DependencyObject d)
		{
			var storageBar = (StorageBar)d;

			UpdateValue( storageBar , storageBar.Value , storageBar.GetOldValue() , false, -1.0 );
		}

		#endregion




		#region 6. Update functions

		/// <summary>
		/// Updates the initial layout of the StorageBar control
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private void UpdateInitialLayout(DependencyObject d)
		{
			var storageBar = d as StorageBar;

			// Retrieve references to visual elements
			storageBar.SetContainerGrid( storageBar.GetTemplateChild( ContainerPartName ) as Grid );

			storageBar.SetValueColumn( storageBar.GetTemplateChild( ValueColumnPartName ) as ColumnDefinition );
			storageBar.SetTrackColumn( storageBar.GetTemplateChild( TrackColumnPartName ) as ColumnDefinition );
			storageBar.SetGapColumn( storageBar.GetTemplateChild( GapColumnPartName ) as ColumnDefinition );

			storageBar.SetValueBarBorder( storageBar.GetTemplateChild( ValueBorderPartName ) as Border );
			storageBar.SetTrackBarBorder( storageBar.GetTemplateChild( TrackBorderPartName ) as Border );

			storageBar.SetBarShape( storageBar.BarShape );

			storageBar.SetValueBarHeight( storageBar.ValueBarHeight );
			storageBar.SetTrackBarHeight( storageBar.TrackBarHeight );
		}




		/// <summary>
		/// Updates the StorageBar control.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private static void UpdateControl(DependencyObject d)
		{
			var storageBar = (StorageBar)d;

			// 1. Update the Bar Heights
			UpdateContainerHeightsAndCorners( storageBar , storageBar.GetValueBarHeight() , storageBar.GetTrackBarHeight() );

			// 2. Set the 3 Column Widths
			UpdateColumnWidths( storageBar , storageBar.Value , storageBar.Minimum , storageBar.Maximum );

			// 3. Update the control's VisualState
			UpdateVisualState( storageBar );
		}




		/// <summary>
		/// Updates the StorageBar Values.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newValue">The new Value</param>
		/// <param name="oldValue">The old Value</param>
		/// <param name="isPercent">Checks if the Percent value is being changed</param>
		/// <param name="newPercent">The new Percent value</param>
		private static void UpdateValue(DependencyObject d , double newValue , double oldValue, bool isPercent, double newPercent)
		{
			var storageBar = (StorageBar)d;

			storageBar.SetOldValue( oldValue );

			double adjustedValue;

			if ( isPercent )
			{
				adjustedValue = storageBar.PercentageToValue( newPercent, storageBar.Minimum, storageBar.Maximum );
			}
			else
			{
				adjustedValue = newValue;
			}

			storageBar.Percent = storageBar.DoubleToPercentage( adjustedValue , storageBar.Minimum , storageBar.Maximum );

			UpdateControl( storageBar );
		}




		/// <summary>
		/// Updates Container Heights and Bar Corner Radii
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="valueBarHeight">The ValueBar Height</param>
		/// <param name="trackBarHeight">The TrackBar Height</param>
		private static void UpdateContainerHeightsAndCorners(DependencyObject d , double valueBarHeight , double trackBarHeight)
		{
			var storageBar = (StorageBar)d;

			Border valueBorder = storageBar.GetValueBarBorder();
			Border trackBorder = storageBar.GetTrackBarBorder();

			Grid containerGrid = storageBar.GetContainerGrid();
			BarShapes barShape = storageBar.GetBarShape();

			// Finds the larger of the two height values
			double calculatedLargerHeight = Math.Max(valueBarHeight, trackBarHeight);
			double calculatedSmallerHeight = Math.Min(valueBarHeight, trackBarHeight);


			if ( valueBorder != null || trackBorder != null || containerGrid != null )
			{
				valueBorder.Height = valueBarHeight;
				trackBorder.Height = trackBarHeight;

				if ( barShape == BarShapes.Round) 
				{

					valueBorder.CornerRadius = new CornerRadius( valueBarHeight / 2 );
					trackBorder.CornerRadius = new CornerRadius( trackBarHeight / 2 );
				}
				else if ( barShape == BarShapes.Soft )
				{

					valueBorder.CornerRadius = new CornerRadius( valueBarHeight / 4 );
					trackBorder.CornerRadius = new CornerRadius( trackBarHeight / 4 );
				}
				else
				{
					valueBorder.CornerRadius = new CornerRadius( 0 );
					trackBorder.CornerRadius = new CornerRadius( 0 );
				}

				containerGrid.Height = calculatedLargerHeight;
			}			

			storageBar.SetGapWidth( calculatedLargerHeight );
			storageBar.SetSmallerHeight( calculatedSmallerHeight );
			
		}




		/// <summary>
		/// Updates Column Widths and Bar Column assignments
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="value">The Value</param>
		/// <param name="minValue">The Minimum value</param>
		/// <param name="maxValue">The Maximum value</param>
		private static void UpdateColumnWidths(DependencyObject d , double value , double minValue , double maxValue)
		{
			var storageBar = (StorageBar)d;

			ColumnDefinition gapColumn = storageBar.GetGapColumn();
			ColumnDefinition valueColumn = storageBar.GetValueColumn();
			ColumnDefinition trackColumn = storageBar.GetTrackColumn();

			Border valueBorder = storageBar.GetValueBarBorder();
			Border trackBorder = storageBar.GetTrackBarBorder();

			if ( gapColumn != null || valueColumn != null || trackColumn != null || valueBorder != null || trackBorder != null )
			{
				Size containerSize = (Size)storageBar.GetContainerSize();

				if ( containerSize == null )
				{
					return;
				}

				if ( containerSize.Width > storageBar.GetTrackBarHeight() || containerSize.Width > storageBar.GetValueBarHeight() )
				{
					double valuePercent = storageBar.DoubleToPercentage(storageBar.Value, storageBar.Minimum, storageBar.Maximum);
					double minPercent   = storageBar.DoubleToPercentage(storageBar.Minimum, storageBar.Minimum, storageBar.Maximum);
					double maxPercent   = storageBar.DoubleToPercentage(storageBar.Maximum, storageBar.Minimum, storageBar.Maximum);

					if ( valuePercent <= minPercent )												// Value is <= Minimum
					{
						gapColumn.Width   = new GridLength( 1 , GridUnitType.Star );
						valueColumn.Width = new GridLength( 1 , GridUnitType.Star );
						trackColumn.Width = new GridLength( 1 , GridUnitType.Star );

						Grid.SetColumnSpan( trackBorder , 3 );
						Grid.SetColumn( trackBorder , 0 );

						valueBorder.Visibility = Visibility.Collapsed;
						trackBorder.Visibility = Visibility.Visible;
					}
					else if ( valuePercent >= maxPercent )											// Value is >= Maximum
					{
						gapColumn.Width   = new GridLength( 1 , GridUnitType.Star );
						valueColumn.Width = new GridLength( 1 , GridUnitType.Star );
						trackColumn.Width = new GridLength( 1 , GridUnitType.Star );

						Grid.SetColumnSpan( valueBorder , 3 );
						Grid.SetColumn( valueBorder , 0 );

						valueBorder.Visibility = Visibility.Visible;
						trackBorder.Visibility = Visibility.Collapsed;
					}
					else
					{
						Grid.SetColumnSpan( valueBorder , 1 );
						Grid.SetColumn( valueBorder , 0 );

						Grid.SetColumnSpan( trackBorder , 1 );
						Grid.SetColumn( trackBorder , 2 );


						valueBorder.Visibility = Visibility.Visible;
						trackBorder.Visibility = Visibility.Visible;


						var valueBarHeight = storageBar.GetValueBarHeight();
						var trackBarHeight = storageBar.GetTrackBarHeight();
						var gapWidth = storageBar.GetGapWidth();

						valueColumn.MaxWidth = containerSize.Width;
						trackColumn.MaxWidth = containerSize.Width;

						bool valueLarger;

						if ( valueBarHeight > trackBarHeight )
						{ 
							valueLarger = true;
						}
						else
						{
							valueLarger = false;
						}


						if ( valuePercent > minPercent && valuePercent <= minPercent + 2.0 )		// Between 0% and 2%
						{
							var interpolatedValueBarHeight  = storageBar.CalculateInterpolatedValue(    storageBar ,
																										minPercent ,
																										storageBar.Percent ,
																										minPercent + 2.0 ,
																										0.0 ,
																										valueBarHeight ,
																										true);

							var interpolatedTrackBarHeight  = storageBar.CalculateInterpolatedValue(    storageBar ,
																										minPercent ,
																										storageBar.Percent ,
																										minPercent + 2.0 ,
																										0.0 ,
																										trackBarHeight ,
																										true);

							var interpolatedGapWidth = gapWidth;

							if ( valueLarger == true )
							{
								interpolatedGapWidth        = storageBar.CalculateInterpolatedValue(    storageBar ,
																										minPercent ,
																										storageBar.Percent ,
																										minPercent + 2.0 ,
																										0.0 ,
																										gapWidth ,
																										true);
							}							
							else
							{
								interpolatedGapWidth        = storageBar.CalculateInterpolatedValue(    storageBar ,
																										minPercent ,
																										storageBar.Percent ,
																										minPercent + 2.0 ,
																										0.0 ,
																										storageBar.GetSmallerHeight() ,
																										true);
							}

							valueColumn.MinWidth = interpolatedValueBarHeight;
							trackColumn.MinWidth = interpolatedTrackBarHeight;

							valueBorder.Height = interpolatedValueBarHeight;
							trackBorder.Height = trackBarHeight;

							double calculatedValueWidth = (valueColumn.MaxWidth / 100) * valuePercent;

							valueColumn.Width = new GridLength( calculatedValueWidth );
							gapColumn.Width = new GridLength( interpolatedGapWidth );
							trackColumn.Width = new GridLength( 1 , GridUnitType.Star );
						}
						else if ( valuePercent >= maxPercent - 1.0 && valuePercent < maxPercent )   // Between 98% and 100%
						{
							var interpolatedValueBarHeight	= storageBar.CalculateInterpolatedValue(	storageBar ,
																										maxPercent - 2.0 ,
																										storageBar.Percent ,
																										maxPercent ,
																										valueBarHeight ,
																										0.0 ,
																										true);

							var interpolatedTrackBarHeight  = storageBar.CalculateInterpolatedValue(    storageBar ,
																										maxPercent - 2.0 ,
																										storageBar.Percent ,
																										maxPercent ,
																										trackBarHeight ,
																										0.0 ,
																										true);

							var interpolatedGapWidth = gapWidth;

							if ( valueLarger == true )
							{
								interpolatedGapWidth = storageBar.CalculateInterpolatedValue(   storageBar ,
																								maxPercent - 2.0 ,
																								storageBar.Percent ,
																								maxPercent ,
																								0.0 ,
																								storageBar.GetSmallerHeight() ,
																								true );
							}
							else
							{
								interpolatedGapWidth = storageBar.CalculateInterpolatedValue(   storageBar ,
																								maxPercent - 2.0 ,
																								storageBar.Percent ,
																								maxPercent ,
																								0.0 ,
																								gapWidth ,
																								true );
							}

							valueColumn.MinWidth = interpolatedValueBarHeight;
							trackColumn.MinWidth = interpolatedTrackBarHeight;

							double calculatedValueWidth = (valueColumn.MaxWidth / 100) * valuePercent;

							valueColumn.Width = new GridLength( calculatedValueWidth );
							trackColumn.Width = new GridLength( 1 , GridUnitType.Star );
							gapColumn.Width = new GridLength( interpolatedGapWidth );

							valueBorder.Height = valueBarHeight;
							trackBorder.Height = interpolatedTrackBarHeight;
						}
						else                                                                        // Between 2% and 98%
						{
							valueColumn.MinWidth = valueBarHeight;
							trackColumn.MinWidth = trackBarHeight;

							double calculatedValueWidth = (valueColumn.MaxWidth / 100) * valuePercent;

							valueColumn.Width = new GridLength( calculatedValueWidth );
							trackColumn.Width = new GridLength( 1 , GridUnitType.Star );

							var interpolatedGapWidth = gapWidth;

							if ( valueLarger == true )
							{
								interpolatedGapWidth = storageBar.CalculateInterpolatedValue( storageBar ,
																										minPercent + 2.0 ,
																										storageBar.Percent ,
																										maxPercent - 2.0 ,
																										gapWidth ,
																										storageBar.GetSmallerHeight() ,
																										true );
							}
							else
							{
								interpolatedGapWidth = storageBar.CalculateInterpolatedValue( storageBar ,
																										minPercent + 2.0 ,
																										storageBar.Percent ,
																										maxPercent - 2.0 ,
																										storageBar.GetSmallerHeight() ,
																										gapWidth ,
																										true );
							}

							gapColumn.Width = new GridLength( interpolatedGapWidth );

							valueBorder.Height = valueBarHeight;
							trackBorder.Height = trackBarHeight;
						}


						
					}
				}
			}
		}




		/// <summary>
		/// Updates the ContainerSize taking in control Padding
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newSize">The new Size</param>
		private static void UpdateContainer(DependencyObject d , Size newSize)
		{
			var storageBar = (StorageBar)d;

			double containerWidth = newSize.Width - ( storageBar.Padding.Left + storageBar.Padding.Right );
			double containerHeight = newSize.Height - ( storageBar.Padding.Top + storageBar.Padding.Bottom );

			storageBar.SetContainerSize( new Size( containerWidth , containerHeight ) );
		}




		/// <summary>
		/// Update the control's VisualState
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private static void UpdateVisualState(DependencyObject d)
		{
			StorageBar storageBar = (StorageBar)d;

			// First is the control is Disabled
			if ( storageBar.IsEnabled == false )
			{
				VisualStateManager.GoToState( storageBar , DisabledStateName , true );
			}
			// Then the control is Enabled
			else
			{
				// Is the Percent value equal to or above the PercentCritical value
				if ( storageBar.Percent >= storageBar.PercentCritical )
				{
					VisualStateManager.GoToState( storageBar , CriticalStateName , true );
				}
				// Is the Percent value equal to or above the PercentCaution value
				else if ( storageBar.Percent >= storageBar.PercentCaution )
				{
					VisualStateManager.GoToState( storageBar , CautionStateName , true );
				}
				// Else we use the Safe State
				else
				{
					VisualStateManager.GoToState( storageBar , SafeStateName , true );
				}
			}
		}

		#endregion




		#region 7. Conversion return functions

		/// <summary>
		/// Converts a value within a specified range to a percentage.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="minValue">The minimum value of the input range.</param>
		/// <param name="maxValue">The maximum value of the input range.</param>
		/// <returns>The percentage value (between 0 and 100).</returns>
		private double DoubleToPercentage(double value , double minValue , double maxValue)
		{
			// Ensure value is within the specified range
			if ( value < minValue )
			{
				return 0.0; // Below the range
			}
			else if ( value > maxValue )
			{
				return 100.0; // Above the range
			}
			else
			{
				// Calculate the normalized value
				var normalizedValue = (value - minValue) / (maxValue - minValue);

				// Convert to percentage
				var percentage = normalizedValue * 100.0;

				double roundedPercentage = Math.Round(percentage , 2, MidpointRounding.ToEven);

				return roundedPercentage;
			}
		}




		/// <summary>
		/// Converts a percentage within a specified range to a value.
		/// </summary>
		/// <param name="percentage">The percentage to convert.</param>
		/// <param name="minValue">The minimum value of the input range.</param>
		/// <param name="maxValue">The maximum value of the input range.</param>
		/// <returns>The percentage value (between 0 and 100).</returns>
		private double PercentageToValue(double percentage , double minValue , double maxValue)
		{
			double convertedValue = percentage * (maxValue - minValue) / 100.0;

			// Ensure the converted value stays within the specified range
			if ( convertedValue < minValue )
			{
				convertedValue = minValue;
			}
			else if ( convertedValue > maxValue )
			{
				convertedValue = maxValue;
			}

			return convertedValue;
		}




		/// <summary>
		/// Calculates an interpolated value based on the provided parameters.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="startValue">The starting value for interpolation.</param>
		/// <param name="value">The current value to interpolate.</param>
		/// <param name="endValue">The ending value for interpolation.</param>
		/// <param name="startOutput">The starting Output value.</param>
		/// <param name="endOutput">The ending Output value.</param>
		/// <param name="useEasing">Indicates whether to apply an easing function.</param>
		/// <returns>The interpolated thickness value.</returns>
		private double CalculateInterpolatedValue(DependencyObject d , double startValue , double value , double endValue , double startOutput , double endOutput , bool useEasing)
		{
			// Ensure that value is within the range [startValue, endValue]
			value = Math.Max( startValue , Math.Min( endValue , value ) );

			// Calculate the interpolation factor (t) between 0 and 1
			var t = (value - startValue) / (endValue - startValue);

			double interpolatedOutput;

			if ( useEasing )
			{
				// Apply an easing function (e.g., quadratic ease-in-out)
				//var easedT = EaseInOutFunction(t);
				var easedT = EaseOutCubic(t);

				// Interpolate the thickness
				interpolatedOutput = startOutput + easedT * ( endOutput - startOutput );
			}
			else
			{
				// Interpolate the thickness
				interpolatedOutput = startOutput + t * ( endOutput - startOutput );
			}

			return interpolatedOutput;
		}




		/// <summary>
		/// Example quadratic ease-in-out function
		/// </summary>
		private double EasingInOutFunction(double t)
		{
			return t < 0.5 ? 2 * t * t : 1 - Math.Pow( -2 * t + 2 , 2 ) / 2;
		}




		/// <summary>
		/// Example ease-out cubic function
		/// </summary>
		static double EaseOutCubic(double t)
		{
			return 1.0 - Math.Pow( 1.0 - t , 3.0 );
		}

		#endregion
	}
}
