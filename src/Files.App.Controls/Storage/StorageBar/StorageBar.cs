// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Controls
{
	// TemplateParts
	[TemplatePart(Name = TemplatePartName_Container, Type = typeof(Grid))]
	[TemplatePart(Name = TemplatePartName_ValueColumn, Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = TemplatePartName__GapColumn, Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = TemplatePartName_TrackColumn, Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = TemplatePartName_ValueBorder, Type = typeof(Border))]
	[TemplatePart(Name = TemplatePartName_TrackBorder, Type = typeof(Border))]
	// VisualStates
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Safe)]
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Caution)]
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Critical)]
	[TemplateVisualState(GroupName = TemplateVisualStateGroupName_ControlStates, Name = TemplateVisualStateName_Disabled)]
	/// <summary>
	/// Represents percentage ring islands.
	/// </summary>
	public partial class StorageBar : RangeBase
	{
		// Constants

		private const string TemplatePartName_Container = "PART_Container";
		private const string TemplatePartName_ValueColumn = "PART_ValueColumn";
		private const string TemplatePartName__GapColumn = "PART_GapColumn";
		private const string TemplatePartName_TrackColumn = "PART_TrackColumn";
		private const string TemplatePartName_ValueBorder = "PART_ValueBar";
		private const string TemplatePartName_TrackBorder = "PART_TrackBar";

		private const string TemplateVisualStateGroupName_ControlStates = "ControlStates";
		private const string TemplateVisualStateName_Safe = "Safe";
		private const string TemplateVisualStateName_Caution = "Caution";
		private const string TemplateVisualStateName_Critical = "Critical";
		private const string TemplateVisualStateName_Disabled = "Disabled";

		// Fields

		Grid _containerGrid = null!;
		ColumnDefinition _valueColumn = null!;
		ColumnDefinition _trackColumn = null!;
		ColumnDefinition _gapColumn = null!;
		Border _valueBarBorder = null!;
		Border _trackBarBorder = null!;

		double _oldValue;
		Size? _containerSize;
		BarShapes _barShape;
		double _gapWidth;
		double _smallerHeight;

		// Constructor

		/// <summary>Initializes an instance of <see cref="StorageBar"/> class.</summary>
		public StorageBar()
		{
			DefaultStyleKey = typeof(StorageBar);
		}

		// Methods

		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_containerGrid = GetTemplateChild(TemplatePartName_Container) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_Container} in the given {nameof(StorageBar)}'s style.");
			_valueColumn = GetTemplateChild(TemplatePartName_ValueColumn) as ColumnDefinition
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ValueColumn} in the given {nameof(StorageBar)}'s style.");
			_trackColumn = GetTemplateChild(TemplatePartName_TrackColumn) as ColumnDefinition
				?? throw new MissingFieldException($"Could not find {TemplatePartName_TrackColumn} in the given {nameof(StorageBar)}'s style.");
			_gapColumn = GetTemplateChild(TemplatePartName__GapColumn) as ColumnDefinition
				?? throw new MissingFieldException($"Could not find {TemplatePartName__GapColumn} in the given {nameof(StorageBar)}'s style.");
			_valueBarBorder = GetTemplateChild(TemplatePartName_ValueBorder) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ValueBorder} in the given {nameof(StorageBar)}'s style.");
			_trackBarBorder = GetTemplateChild(TemplatePartName_TrackBorder) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_TrackBorder} in the given {nameof(StorageBar)}'s style.");

			_barShape = BarShape;

			SizeChanged += StorageBar_SizeChanged;
			Unloaded += StorageBar_Unloaded;
			IsEnabledChanged += StorageBar_IsEnabledChanged;
		}

		// Methods

		private void UpdateControl()
		{
			UpdateContainerHeightsAndCorners();
			UpdateColumnWidths();
			UpdateVisualState();
		}

		private void UpdateValue(double newValue, double oldValue, bool isPercent, double newPercent)
		{
			_oldValue = oldValue;

			var adjustedValue = isPercent ? StorageControlsHelpers.PercentageToValue(newPercent, Minimum, Maximum) : newValue;
			Percent = StorageControlsHelpers.DoubleToPercentage(adjustedValue, Minimum, Maximum);

			UpdateControl();
		}

		private void UpdateContainerHeightsAndCorners()
		{
			// Finds the larger of the two height values
			double calculatedLargerHeight = Math.Max(ValueBarHeight, TrackBarHeight);
			double calculatedSmallerHeight = Math.Min(ValueBarHeight, TrackBarHeight);

			if (_valueBarBorder != null && _trackBarBorder != null && _containerGrid != null)
			{
				_valueBarBorder.Height = ValueBarHeight;
				_trackBarBorder.Height = TrackBarHeight;

				if (_barShape is BarShapes.Round)
				{
					_valueBarBorder.CornerRadius = new(ValueBarHeight / 2);
					_trackBarBorder.CornerRadius = new(TrackBarHeight / 2);
				}
				else if (_barShape is BarShapes.Soft)
				{
					_valueBarBorder.CornerRadius = new(ValueBarHeight / 4);
					_trackBarBorder.CornerRadius = new(TrackBarHeight / 4);
				}
				else
				{
					_valueBarBorder.CornerRadius = new(0);
					_trackBarBorder.CornerRadius = new(0);
				}

				_containerGrid.Height = calculatedLargerHeight;
			}

			_gapWidth = calculatedLargerHeight;
			_smallerHeight = calculatedSmallerHeight;

		}

		private void UpdateColumnWidths()
		{
			if (_gapColumn != null && _valueColumn != null && _trackColumn != null && _valueBarBorder != null && _trackBarBorder != null)
			{
				if (_containerSize is not Size containerSize)
					return;

				if (containerSize.Width > TrackBarHeight || containerSize.Width > ValueBarHeight)
				{
					double valuePercent = StorageControlsHelpers.DoubleToPercentage(Value, Minimum, Maximum);
					double minPercent = StorageControlsHelpers.DoubleToPercentage(Minimum, Minimum, Maximum);
					double maxPercent = StorageControlsHelpers.DoubleToPercentage(Maximum, Minimum, Maximum);

					if (valuePercent <= minPercent)
					{
						_gapColumn.Width = new(1, GridUnitType.Star);
						_valueColumn.Width = new(1, GridUnitType.Star);
						_trackColumn.Width = new(1, GridUnitType.Star);

						Grid.SetColumnSpan(_trackBarBorder, 3);
						Grid.SetColumn(_trackBarBorder, 0);

						_valueBarBorder.Visibility = Visibility.Collapsed;
						_trackBarBorder.Visibility = Visibility.Visible;
					}
					else if (valuePercent >= maxPercent)
					{
						_gapColumn.Width = new(1, GridUnitType.Star);
						_valueColumn.Width = new(1, GridUnitType.Star);
						_trackColumn.Width = new(1, GridUnitType.Star);

						Grid.SetColumnSpan(_valueBarBorder, 3);
						Grid.SetColumn(_valueBarBorder, 0);

						_valueBarBorder.Visibility = Visibility.Visible;
						_trackBarBorder.Visibility = Visibility.Collapsed;
					}
					else
					{
						Grid.SetColumnSpan(_valueBarBorder, 1);
						Grid.SetColumn(_valueBarBorder, 0);

						Grid.SetColumnSpan(_trackBarBorder, 1);
						Grid.SetColumn(_trackBarBorder, 2);

						_valueBarBorder.Visibility = Visibility.Visible;
						_trackBarBorder.Visibility = Visibility.Visible;

						var valueBarHeight = ValueBarHeight;
						var trackBarHeight = TrackBarHeight;
						var gapWidth = _gapWidth;

						_valueColumn.MaxWidth = containerSize.Width;
						_trackColumn.MaxWidth = containerSize.Width;

						var valueLarger = valueBarHeight > trackBarHeight;

						if (valuePercent > minPercent && valuePercent <= minPercent + 2.0)  // Between 0% and 2%
						{
							var interpolatedValueBarHeight = StorageControlsHelpers.CalculateInterpolatedValue(
								minPercent,
								Percent,
								minPercent + 2.0,
								0.0,
								valueBarHeight,
								true);

							var interpolatedTrackBarHeight = StorageControlsHelpers.CalculateInterpolatedValue(
								minPercent,
								Percent,
								minPercent + 2.0,
								0.0,
								trackBarHeight,
								true);

							var interpolatedGapWidth = valueLarger
								?  StorageControlsHelpers.CalculateInterpolatedValue(
									minPercent,
									Percent,
									minPercent + 2.0,
									0.0,
									gapWidth,
									true)
								: StorageControlsHelpers.CalculateInterpolatedValue(
									minPercent,
									Percent,
									minPercent + 2.0,
									0.0,
									_smallerHeight,
									true);

							_valueColumn.MinWidth = interpolatedValueBarHeight;
							_trackColumn.MinWidth = interpolatedTrackBarHeight;

							_valueBarBorder.Height = interpolatedValueBarHeight;
							_trackBarBorder.Height = trackBarHeight;

							var calculatedValueWidth = (_valueColumn.MaxWidth / 100) * valuePercent;

							_valueColumn.Width = new(calculatedValueWidth);
							_gapColumn.Width = new(interpolatedGapWidth);
							_trackColumn.Width = new(1, GridUnitType.Star);
						}
						else if (valuePercent >= maxPercent - 1.0 && valuePercent < maxPercent)   // Between 98% and 100%
						{
							var interpolatedValueBarHeight = StorageControlsHelpers.CalculateInterpolatedValue(
								maxPercent - 2.0,
								Percent,
								maxPercent,
								valueBarHeight,
								0.0,
								true);

							var interpolatedTrackBarHeight = StorageControlsHelpers.CalculateInterpolatedValue(
								maxPercent - 2.0,
								Percent,
								maxPercent,
								trackBarHeight,
								0.0,
								true);

							var interpolatedGapWidth = valueLarger
								? StorageControlsHelpers.CalculateInterpolatedValue(
									maxPercent - 2.0,
									Percent,
									maxPercent,
									0.0,
									_smallerHeight,
									true)
								: StorageControlsHelpers.CalculateInterpolatedValue(
									maxPercent - 2.0,
									Percent,
									maxPercent,
									0.0,
									gapWidth,
									true);

							_valueColumn.MinWidth = interpolatedValueBarHeight;
							_trackColumn.MinWidth = interpolatedTrackBarHeight;

							var calculatedValueWidth = (_valueColumn.MaxWidth / 100) * valuePercent;

							_valueColumn.Width = new(calculatedValueWidth);
							_trackColumn.Width = new(1, GridUnitType.Star);
							_gapColumn.Width = new(interpolatedGapWidth);

							_valueBarBorder.Height = valueBarHeight;
							_trackBarBorder.Height = interpolatedTrackBarHeight;
						}
						else  // Between 2% and 98%
						{
							_valueColumn.MinWidth = valueBarHeight;
							_trackColumn.MinWidth = trackBarHeight;

							double calculatedValueWidth = (_valueColumn.MaxWidth / 100) * valuePercent;

							_valueColumn.Width = new(calculatedValueWidth);
							_trackColumn.Width = new(1, GridUnitType.Star);

							var interpolatedGapWidth = valueLarger
								? StorageControlsHelpers.CalculateInterpolatedValue(
									minPercent + 2.0,
									Percent,
									maxPercent - 2.0,
									gapWidth,
									_smallerHeight,
									true)
								: StorageControlsHelpers.CalculateInterpolatedValue(
									minPercent + 2.0,
									Percent,
									maxPercent - 2.0,
									_smallerHeight,
									gapWidth,
									true);

							_gapColumn.Width = new(interpolatedGapWidth);

							_valueBarBorder.Height = valueBarHeight;
							_trackBarBorder.Height = trackBarHeight;
						}
					}
				}
			}
		}

		private void UpdateContainer(Size newSize)
		{
			double containerWidth = newSize.Width - (Padding.Left + Padding.Right);
			double containerHeight = newSize.Height - (Padding.Top + Padding.Bottom);

			_containerSize = new(containerWidth, containerHeight);
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

		// Event methods

		private void StorageBar_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateControl();
		}

		private void StorageBar_SizeChanged(object sender, SizeChangedEventArgs e)
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

			UpdateContainer(minSize);
			UpdateControl();
		}

		private void StorageBar_Unloaded(object sender, RoutedEventArgs e)
		{
			SizeChanged -= StorageBar_SizeChanged;
			Unloaded -= StorageBar_Unloaded;
			IsEnabledChanged -= StorageBar_IsEnabledChanged;
		}
	}
}
