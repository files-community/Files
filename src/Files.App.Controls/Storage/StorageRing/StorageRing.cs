// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Files.App.Controls
{
	// TemplateParts
	[TemplatePart(Name = TemplatePartName_Container, Type = typeof(Grid))]
	[TemplatePart(Name = TemplatePartName_ValueRingShape, Type = typeof(RingShape))]
	[TemplatePart(Name = TemplatePartName_TrackRingShape, Type = typeof(RingShape))]
	// VisualStates
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Safe)]
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Caution)]
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Critical)]
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Disabled)]
	/// <summary>
	/// Represents percentage bar islands.
	/// </summary>
	public partial class StorageRing : RangeBase
	{
		private const string TemplatePartName_Container = "PART_Container";
		private const string TemplatePartName_ValueRingShape = "PART_ValueRingShape";
		private const string TemplatePartName_TrackRingShape = "PART_TrackRingShape";

		private const string TemplateVisualStateGroupName_ControlStates = "ControlStates";
		private const string TemplateVisualStateName_Safe = "Safe";
		private const string TemplateVisualStateName_Caution = "Caution";
		private const string TemplateVisualStateName_Critical = "Critical";
		private const string TemplateVisualStateName_Disabled = "Disabled";

		private const double DegreesToRadians = Math.PI / 180;
		private const double minSize = 8;

		// Fields

		private Grid _containerGrid = null!;
		private RingShape _valueRingShape = null!;
		private RingShape _trackRingShape = null!;

		private double _containerSize;          // Size of the inner container after padding
		private double _containerCenter;        // Center X and Y value of the inner container
		private double _sharedRadius;           // Radius to be shared by both rings (smaller of the two)

		private double _oldValue;               // Stores the previous Value
		private double _oldValueAngle;          // Stored the old ValueAngle

		private double _valueRingThickness;     // The stored value ring thickness
		private double _trackRingThickness;     // The stored track ring thickness
		private ThicknessCheck _thicknessCheck; // Determines how the two ring thicknesses compare
		private double _largerThickness;        // The larger of the two ring thicknesses
		private double _smallerThickness;       // The smaller of the two ring thicknesses

		private RectangleGeometry? _clipRect;   // Clipping RectangleGeometry for the canvas

		private double _normalizedMinAngle;     // Stores the normalized Minimum Angle
		private double _normalizedMaxAngle;     // Stores the normalized Maximum Angle
		private double _gapAngle;               // Stores the angle to be used to separate Value and Track rings
		private double _validStartAngle;        // The validated StartAngle

		// Constructor

		/// <summary>Initializes an instance of <see cref="StorageRing"/> class.</summary>
		public StorageRing()
		{
			DefaultStyleKey = typeof(StorageRing);
		}

		// Methods

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Retrieve references to visual elements
			_containerGrid = GetTemplateChild(TemplatePartName_Container) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_Container} in the given {nameof(StorageRing)}'s style.");
			_valueRingShape = GetTemplateChild(TemplatePartName_ValueRingShape) as RingShape
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ValueRingShape} in the given {nameof(StorageRing)}'s style.");
			_trackRingShape = GetTemplateChild(TemplatePartName_TrackRingShape) as RingShape
				?? throw new MissingFieldException($"Could not find {TemplatePartName_TrackRingShape} in the given {nameof(StorageRing)}'s style.");

			// Initialize the ring
			UpdateValues(Value, 0.0, false, -1.0);
			UpdateRings();

			// Hook control events
			SizeChanged += StorageRing_SizeChanged;
			IsEnabledChanged += StorageRing_IsEnabledChanged;
			Unloaded += StorageRing_Unloaded;
		}

		private void UpdateRings()
		{
			if (_valueRingShape is null || _trackRingShape is null)
				return;

			// Update every detail of the control
			UpdateContainerCenterAndSizes();
			UpdateNormalizedAngles(MinAngle, MaxAngle);
			UpdateRingSizes(_valueRingShape, _trackRingShape);
			UpdateRadii(_sharedRadius, false);
			UpdateRadii(_sharedRadius, true);
			UpdateGapAngle();
			UpdateRingLayouts(_valueRingShape, _trackRingShape);
			UpdateRingAngles(_valueRingShape, _trackRingShape);
			UpdateRingStrokes(_valueRingShape, _trackRingShape);
			UpdateRingThickness(ValueRingThickness, false);
			UpdateRingThickness(TrackRingThickness, true);
			UpdateVisualState();
		}

		#region Update methods
		private void UpdateContainerCenterAndSizes()
		{
			var borderThickness = BorderThickness;

			var borderWidth = borderThickness.Left + borderThickness.Right;
			var borderHeight = borderThickness.Top + borderThickness.Bottom;

			// Set Container Size
			double correctedWidth = Width - (borderWidth * 2) - (Padding.Left + Padding.Right);
			double correctedHeight = Height - (borderHeight * 2) - (Padding.Top + Padding.Bottom);
			double check = Math.Min(correctedWidth, correctedHeight);
			_containerSize = check < minSize ? minSize : check;
			AdjustedSize = _containerSize;

			// Set Container Center
			_containerCenter = (AdjustedSize / 2);

			// Set Clipping Rectangle
			RectangleGeometry rectGeo = new() { Rect = new(0 - borderWidth, 0 - borderHeight, AdjustedSize + borderWidth, AdjustedSize + borderHeight) };
			_clipRect = rectGeo;

			// Get Container
			var container = _containerGrid;

			// If the Container is not null.
			if (container != null)
			{
				// Set the _container width and height to the adjusted size (AdjustedSize).
				container.Width = AdjustedSize;
				container.Height = AdjustedSize;
				container.Clip = _clipRect;
			}
		}

		private void UpdateNormalizedAngles(double minAngle, double maxAngle)
		{
			var result = StorageControlsHelpers.CalculateModulus(minAngle, 360);

			if (result >= 180)
				result -= 360;

			_normalizedMinAngle = result;

			result = StorageControlsHelpers.CalculateModulus(maxAngle, 360);

			if (result < 180)
				result += 360;

			if (result > _normalizedMinAngle + 360)
				result -= 360;

			_normalizedMaxAngle = result;
		}

		private void UpdateRingSizes(RingShape valueRingShape, RingShape trackRingShape)
		{
			if (valueRingShape is null || trackRingShape is null)
				return;

			// Set sizes for the rings as needed
			if (_thicknessCheck is ThicknessCheck.Value)
			{
				valueRingShape.Width = _containerSize;
				valueRingShape.Height = _containerSize;

				trackRingShape.Width = _containerSize - (_largerThickness / 2);
				trackRingShape.Height = _containerSize - (_largerThickness / 2);
			}
			else if (_thicknessCheck is ThicknessCheck.Track)
			{
				valueRingShape.Width = _containerSize - (_largerThickness / 2);
				valueRingShape.Height = _containerSize - (_largerThickness / 2);

				trackRingShape.Width = _containerSize;
				trackRingShape.Height = _containerSize;
			}
			else // ThicknessCheck == Equal
			{
				valueRingShape.Width = _containerSize;
				valueRingShape.Height = _containerSize;

				trackRingShape.Width = _containerSize;
				trackRingShape.Height = _containerSize;
			}

			valueRingShape.UpdateLayout();
			trackRingShape.UpdateLayout();
		}

		private void UpdateRadii(double newRadius, bool isTrack)
		{
			double valueRingThickness = _valueRingThickness;
			double trackRingThickness = _trackRingThickness;

			// Limit the Thickness values to no more than 1/5 of the container size
			if (isTrack)
				trackRingThickness = newRadius > (AdjustedSize / 5) ? (AdjustedSize / 5) : newRadius;
			else
				valueRingThickness = newRadius > (AdjustedSize / 5) ? (AdjustedSize / 5) : newRadius;

			// If both Rings have Equal thickness, use 0; otherwise, use the larger thickness to adjust the size
			double check = (AdjustedSize / 2) - (_thicknessCheck is ThicknessCheck.Equal ? 0 : _largerThickness / 2);
			double minSize = 4;

			_sharedRadius = check <= minSize ? minSize : check;
		}

		private void UpdateGapAngle()
		{
			double angle = StorageControlsHelpers.GapThicknessToAngle(_sharedRadius, (_largerThickness * 0.75));
			_gapAngle = angle;
		}

		private void UpdateValues(double newValue, double oldValue, bool percentChanged, double newPercent)
		{
			UpdateNormalizedAngles(MinAngle, MaxAngle);

			double adjustedValue;

			if (percentChanged)
			{
				var percentToValue = StorageControlsHelpers.PercentageToValue(newPercent, Minimum, Maximum);
				adjustedValue = percentToValue;
			}
			else
			{
				adjustedValue = newValue;
			}

			ValueAngle = DoubleToAngle(adjustedValue, Minimum, Maximum, _normalizedMinAngle, _normalizedMaxAngle);
			Percent = StorageControlsHelpers.DoubleToPercentage(adjustedValue, Minimum, Maximum);

			_oldValue = oldValue;
			_oldValueAngle = DoubleToAngle(oldValue, Minimum, Maximum, _normalizedMinAngle, _normalizedMaxAngle);
		}

		private void UpdateRingLayouts(RingShape valueRingShape, RingShape trackRingShape)
		{
			if (valueRingShape is null || trackRingShape is null)
				return;

			valueRingShape.RadiusWidth = _sharedRadius;
			valueRingShape.RadiusHeight = _sharedRadius;

			trackRingShape.RadiusWidth = _sharedRadius;
			trackRingShape.RadiusHeight = _sharedRadius;

			// Apply Width and Heights
			valueRingShape.Width = AdjustedSize;
			valueRingShape.Height = AdjustedSize;

			trackRingShape.Width = AdjustedSize;
			trackRingShape.Height = AdjustedSize;
		}

		private void UpdateRingAngles(RingShape valueRingShape, RingShape trackRingShape)
		{
			if (valueRingShape is null || trackRingShape is null)
				return;

			double valueStartAngle = _normalizedMinAngle;
			double valueEndAngle;
			double trackStartAngle = _normalizedMaxAngle;
			double trackEndAngle;

			//
			// We get percentage values to use for manipulating how we draw the rings.
			var minPercent = StorageControlsHelpers.DoubleToPercentage(Minimum, Minimum, Maximum);
			var maxPercent = StorageControlsHelpers.DoubleToPercentage(Maximum, Minimum, Maximum);
			var percent = Percent;

			//
			// Percent is below or at its Minimum
			if (percent <= minPercent)
			{
				valueEndAngle = _normalizedMinAngle;

				trackStartAngle = _normalizedMaxAngle - 0.01;
				trackEndAngle = _normalizedMinAngle;
			}
			//
			// Percent is between it's Minimum and its Minimum + 2 (between 0% and 2%)
			else if (percent > minPercent && percent < minPercent + 2.0)
			{
				valueEndAngle = ValueAngle;

				double interpolatedStartTo;
				double interpolatedEndTo;

				//
				// We need to interpolate the track start and end angles between pRing.Minimum and pRing.Minimum + 0.75
				interpolatedStartTo = StorageControlsHelpers.GetAdjustedAngle(
					minPercent,
					percent,
					minPercent + 2.0,
					_normalizedMinAngle,
					_normalizedMinAngle + _gapAngle,
					ValueAngle,
					true);

				if (StorageControlsHelpers.IsFullCircle(_normalizedMinAngle, _normalizedMaxAngle) == true)
				{
					interpolatedEndTo = StorageControlsHelpers.GetAdjustedAngle(
						minPercent,
						percent,
						minPercent + 2.0,
						_normalizedMaxAngle,
						_normalizedMaxAngle - (_gapAngle + ValueAngle),
						ValueAngle,
						true);
				}
				else
				{
					interpolatedEndTo = _normalizedMaxAngle;
				}

				trackStartAngle = interpolatedEndTo;
				trackEndAngle = interpolatedStartTo;
			}
			//
			// Percent is at or above its Maximum value
			else if (percent >= maxPercent)
			{
				valueEndAngle = _normalizedMaxAngle;

				trackStartAngle = _normalizedMaxAngle;
				trackEndAngle = _normalizedMinAngle;
			}
			//
			// Any value between the Minimum and the Maximum value
			else
			{
				valueEndAngle = ValueAngle;

				if (StorageControlsHelpers.IsFullCircle(MinAngle, MaxAngle) == true)
				{
					trackStartAngle = _normalizedMaxAngle - _gapAngle;

					//
					// When the trackRing's EndAngle meets or exceeds its adjusted StartAngle
					if (ValueAngle > (_normalizedMaxAngle - (_gapAngle * 2)))
					{
						trackEndAngle = _normalizedMaxAngle - (_gapAngle - 0.0001);
					}
					else
					{
						// We take the MaxAngle - the GapAngle, then minus the ValueAngle from it
						trackEndAngle = (_normalizedMinAngle + _gapAngle) - (_normalizedMinAngle - ValueAngle);
					}
				}
				else
				{
					trackStartAngle = _normalizedMaxAngle;

					//
					// When the trackRing's EndAngle meets or exceeds its adjusted StartAngle
					if (ValueAngle > (_normalizedMaxAngle - (_gapAngle / 20)))
					{
						trackEndAngle = (_normalizedMaxAngle - 0.0001);
					}
					else
					{
						// We take the MaxAngle - the GapAngle, then minus the ValueAngle from it
						trackEndAngle = (_normalizedMinAngle + (_gapAngle - (_normalizedMinAngle - ValueAngle)));
					}
				}
			}

			valueRingShape.StartAngle = valueStartAngle;
			trackRingShape.StartAngle = trackStartAngle;

			valueRingShape.EndAngle = valueEndAngle;
			trackRingShape.EndAngle = trackEndAngle;
		}

		private void UpdateRingStrokes(RingShape valueRingShape, RingShape trackRingShape)
		{
			if (valueRingShape is null || trackRingShape is null)
				return;

			var normalizedMinAngle = _normalizedMinAngle;
			var normalizedMaxAngle = _normalizedMaxAngle;

			// We get percentage values to use for manipulating how we draw the rings.
			var minPercent = StorageControlsHelpers.DoubleToPercentage(Minimum, Minimum, Maximum);
			var maxPercent = StorageControlsHelpers.DoubleToPercentage(Maximum, Minimum, Maximum);
			var percent = Percent;

			// Percent is below or at its Minimum
			if (percent <= minPercent)
			{
				valueRingShape.StrokeThickness = 0;
				trackRingShape.StrokeThickness = _trackRingThickness;
			}
			// Percent is between it's Minimum and its Minimum + 2.0 (between 0% and 2%)
			else if (percent > minPercent && percent < minPercent + 2.0)
			{
				valueRingShape.StrokeThickness = StorageControlsHelpers.GetThicknessTransition(
					minPercent,
					percent,
					minPercent + 2.0,
					0.0,
					_valueRingThickness,
					true);

				trackRingShape.StrokeThickness = _trackRingThickness;
			}
			//
			// Percent is at or above its Maximum value
			else if (percent >= maxPercent)
			{
				valueRingShape.StrokeThickness = _valueRingThickness;
				trackRingShape.StrokeThickness = 0;
			}
			//
			// Any percent value between the Minimum + 2 and the Maximum value
			else
			{
				valueRingShape.StrokeThickness = ValueRingThickness;

				if (StorageControlsHelpers.IsFullCircle(normalizedMinAngle, normalizedMaxAngle) == true)
				{
					if (ValueAngle > (normalizedMaxAngle + 1.0) - (_gapAngle * 2))
					{
						valueRingShape.StrokeThickness = _valueRingThickness;

						trackRingShape.StrokeThickness = StorageControlsHelpers.GetThicknessTransition(
							(normalizedMaxAngle + 0.1) - (_gapAngle * 2),
							ValueAngle, (normalizedMaxAngle) - (_gapAngle),
							_trackRingThickness,
							0.0,
							true);
					}
					else
					{
						valueRingShape.StrokeThickness = _valueRingThickness;
						trackRingShape.StrokeThickness = _trackRingThickness;
					}
				}
				else
				{
					if (ValueAngle > (normalizedMaxAngle - _gapAngle))
					{
						valueRingShape.StrokeThickness = _valueRingThickness;
						trackRingShape.StrokeThickness = StorageControlsHelpers.GetThicknessTransition(
							(normalizedMaxAngle + 0.1) - (_gapAngle / 2),
							ValueAngle, (normalizedMaxAngle) - (_gapAngle / 2),
							_trackRingThickness,
							0.0,
							true);
					}
					else
					{
						valueRingShape.StrokeThickness = _valueRingThickness;
						trackRingShape.StrokeThickness = _trackRingThickness;
					}
				}
			}
			;
		}

		private void UpdateRingThickness(double newThickness, bool isTrack)
		{
			if (isTrack)
				_trackRingThickness = newThickness;
			else
				_valueRingThickness = newThickness;

			if (_valueRingThickness > _trackRingThickness)
				_thicknessCheck = ThicknessCheck.Value;
			else if (_valueRingThickness < _trackRingThickness)
				_thicknessCheck = ThicknessCheck.Track;
			else
				_thicknessCheck = ThicknessCheck.Equal;

			if (_thicknessCheck is ThicknessCheck.Value)
			{
				_largerThickness = _valueRingThickness;
				_smallerThickness = _trackRingThickness;
			}
			else if (_thicknessCheck is ThicknessCheck.Track)
			{
				_largerThickness = _trackRingThickness;
				_smallerThickness = _valueRingThickness;
			}
			else // ThicknessCheck == Equal
			{
				_largerThickness = _valueRingThickness;
				_smallerThickness = _valueRingThickness;
			}
		}

		private void UpdateVisualState()
		{
			VisualStateManager.GoToState(
				this,
				IsEnabled
					? Percent >= PercentCritical
						? TemplateVisualStateName_Critical
						: Percent >= PercentCaution
							? TemplateVisualStateName_Caution
							: TemplateVisualStateName_Safe
					: TemplateVisualStateName_Disabled,
				true);
		}

		private void ValidateStartAngle(double startAngle)
		{
			_validStartAngle = startAngle switch
			{
				_ when startAngle >= _normalizedMaxAngle => _normalizedMaxAngle,
				_ when startAngle <= _normalizedMinAngle => _normalizedMinAngle,
				_ => startAngle,
			};
		}
		#endregion

		private double DoubleToAngle(double value, double minValue, double maxValue, double minAngle, double maxAngle)
		{
			// Converts a value within a specified range to an angle within another specified range.

			// If value is below the Minimum set
			if (value < minValue)
				return minAngle;

			// If value is above the Maximum set
			if (value > maxValue)
				return maxAngle;

			// Calculate the normalized value
			double normalizedValue = (value - minValue) / (maxValue - minValue);

			// Determine the angle range
			double angleRange = MaxAngle - MinAngle;

			// Calculate the actual angle
			double angle = MinAngle + (normalizedValue * angleRange);

			return angle;
		}

		// Event methods

		private void StorageRing_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Size minSize;

			if (DesiredSize.Width < MinWidth || DesiredSize.Height < MinHeight ||
				e.NewSize.Width < MinWidth || e.NewSize.Height < MinHeight)
			{
				Width = MinWidth;
				Height = MinHeight;

				minSize = new Size(MinWidth, MinHeight);
			}
			else
			{
				minSize = e.NewSize;
			}

			UpdateContainerCenterAndSizes();
			UpdateRings();
		}

		private void StorageRing_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateVisualState();
		}

		private void StorageRing_Unloaded(object sender, RoutedEventArgs e)
		{
			SizeChanged -= StorageRing_SizeChanged;
			IsEnabledChanged -= StorageRing_IsEnabledChanged;
			Unloaded -= StorageRing_Unloaded;
		}
	}
}
