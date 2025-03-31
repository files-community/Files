// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	/// <summary>
	/// Helpers for <see cref="StorageRing"/> and <see cref="StorageBar"/>.
	/// </summary>
	public static partial class StorageControlsHelpers
	{
		/// <summary>
		/// Calculates the modulus of a number with respect to a divider.
		/// The result is always positive or zero, regardless of the input values.
		/// </summary>
		/// <param name="number">The input number.</param>
		/// <param name="divider">The divider (non-zero).</param>
		/// <returns>The positive modulus result.</returns>
		public static double CalculateModulus(double number, double divider)
		{
			// Calculate the modulus
			var result = number % divider;

			// Ensure the result is positive or zero
			result = result < 0 ? result + divider : result;

			return result;
		}

		/// <summary>
		/// Calculates an interpolated thickness value based on the provided parameters.
		/// </summary>
		/// <param name="startValue">The starting value for interpolation.</param>
		/// <param name="value">The current value to interpolate.</param>
		/// <param name="endValue">The ending value for interpolation.</param>
		/// <param name="startThickness">The starting thickness value.</param>
		/// <param name="endThickness">The ending thickness value.</param>
		/// <param name="useEasing">Indicates whether to apply an easing function.</param>
		/// <returns>The interpolated thickness value.</returns>
		public static double GetThicknessTransition(double startValue, double value, double endValue, double startThickness, double endThickness, bool useEasing)
		{
			// Ensure that value is within the range [startValue, endValue]
			value = Math.Max(startValue, Math.Min(endValue, value));

			// Calculate the interpolation factor (t) between 0 and 1
			var t = (value - startValue) / (endValue - startValue);

			double interpolatedThickness;

			if (useEasing)
			{
				// Apply an easing function (e.g., quadratic ease-in-out)
				var easedT = EaseOutCubic(t);

				// Interpolate the thickness
				interpolatedThickness = startThickness + easedT * (endThickness - startThickness);
			}
			else
			{
				// Interpolate the thickness
				interpolatedThickness = startThickness + t * (endThickness - startThickness);
			}

			return interpolatedThickness;
		}

		/// <summary>
		/// Calculates an interpolated angle based on the provided parameters.
		/// </summary>
		/// <param name="startValue">The starting value for interpolation.</param>
		/// <param name="value">The current value to interpolate.</param>
		/// <param name="endValue">The ending value for interpolation.</param>
		/// <param name="startAngle">The starting angle value.</param>
		/// <param name="endAngle">The ending angle value.</param>
		/// <param name="valueAngle">The angle corresponding to the current value.</param>
		/// <param name="useEasing">Indicates whether to apply an easing function.</param>
		/// <returns>The interpolated angle value.</returns>
		public static double GetAdjustedAngle(double startValue, double value, double endValue, double startAngle, double endAngle, double valueAngle, bool useEasing)
		{
			// Ensure that value is within the range [startValue, endValue]
			value = Math.Max(startValue, Math.Min(endValue, value));

			// Calculate the interpolation factor (t) between 0 and 1
			var t = (value - startValue) / (endValue - startValue);

			double interpolatedAngle;

			if (useEasing)
			{
				// Apply an easing function
				var easedT = StorageControlsHelpers.EaseOutCubic(t);

				// Interpolate the angle
				interpolatedAngle = startAngle + easedT * (endAngle - startAngle);
			}
			else
			{
				// Interpolate the angle
				interpolatedAngle = startAngle + t * (endAngle - startAngle);
			}

			return interpolatedAngle;
		}

		/// <summary>
		/// Converts a value within a specified range to a percentage.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="minValue">The minimum value of the input range.</param>
		/// <param name="maxValue">The maximum value of the input range.</param>
		/// <returns>The percentage value (between 0 and 100).</returns>
		public static double DoubleToPercentage(double value, double minValue, double maxValue)
		{
			// Ensure value is within the specified range
			if (value < minValue)
			{
				return 0.0; // Below the range
			}
			else if (value > maxValue)
			{
				return 100.0; // Above the range
			}
			else
			{
				// Calculate the normalized value
				var normalizedValue = (value - minValue) / (maxValue - minValue);

				// Convert to percentage
				var percentage = normalizedValue * 100.0;

				double roundedPercentage = Math.Round(percentage, 2, MidpointRounding.ToEven);
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
		public static double PercentageToValue(double percentage, double minValue, double maxValue)
		{
			double convertedValue = percentage * (maxValue - minValue) / 100.0;

			// Ensure the converted value stays within the specified range
			if (convertedValue < minValue)
				convertedValue = minValue;
			else if (convertedValue > maxValue)
				convertedValue = maxValue;

			return convertedValue;
		}

		/// <summary>
		/// Calculates the total angle needed to accommodate a gap between two strokes around a circle.
		/// </summary>
		/// <param name="thickness">The Thickness radius to measure.</param>
		/// <param name="radius">The radius of the rings.</param>
		/// <returns>The gap angle (sum of angles for the larger and smaller strokes).</returns>
		public static double GapThicknessToAngle(double radius, double thickness)
		{
			if (radius > 0 && thickness > 0)
			{
				// Calculate the maximum number of circles
				double n = Math.PI * (radius / thickness);

				// Calculate the angle between each small circle
				double angle = 360.0 / n;

				return angle;
			}

			return 0;
		}

		/// <summary>
		/// Calculates an adjusted angle using linear interpolation (lerp) between the start and end angles.
		/// </summary>
		/// <param name="startAngle">The initial angle.</param>
		/// <param name="endAngle">The final angle.</param>
		/// <param name="valueAngle">A value between 0 and 1 representing the interpolation factor.</param>
		/// <returns>The adjusted angle based on linear interpolation.</returns>
		public static double GetInterpolatedAngle(double startAngle, double endAngle, double valueAngle)
		{
			// Linear interpolation formula (lerp): GetInterpolatedAngle = (startAngle + valueAngle) * (endAngle - startAngle)
			return (startAngle + valueAngle) * (endAngle - startAngle);
		}

		/// <summary>
		/// Checks True if the Angle range is a Full Circle
		/// </summary>
		/// <param name="MinAngle"></param>
		/// <param name="MaxAngle"></param>
		/// <returns></returns>
		public static bool IsFullCircle(double MinAngle, double MaxAngle)
		{
			// Calculate the absolute difference between angles
			double angleDifference = Math.Abs(MaxAngle - MinAngle);

			// Check if the angle difference is equal to 360 degrees
			//return angleDifference == 360;

			// Changed to this as suggested by Marcel
			return Math.Abs(angleDifference - 360) < Double.Epsilon;
		}

		/// <summary>
		/// Calculates an interpolated value based on the provided parameters.
		/// </summary>
		/// <param name="startValue">The starting value for interpolation.</param>
		/// <param name="value">The current value to interpolate.</param>
		/// <param name="endValue">The ending value for interpolation.</param>
		/// <param name="startOutput">The starting Output value.</param>
		/// <param name="endOutput">The ending Output value.</param>
		/// <param name="useEasing">Indicates whether to apply an easing function.</param>
		/// <returns>The interpolated thickness value.</returns>
		public static double CalculateInterpolatedValue(double startValue, double value, double endValue, double startOutput, double endOutput, bool useEasing)
		{
			// Ensure that value is within the range [startValue, endValue]
			value = Math.Max(startValue, Math.Min(endValue, value));

			// Calculate the interpolation factor (t) between 0 and 1
			var t = (value - startValue) / (endValue - startValue);

			double interpolatedOutput;

			if (useEasing)
			{
				// Apply an easing function (e.g., quadratic ease-in-out)
				//var easedT = EaseInOutFunction(t);
				var easedT = EaseOutCubic(t);

				// Interpolate the thickness
				interpolatedOutput = startOutput + easedT * (endOutput - startOutput);
			}
			else
			{
				// Interpolate the thickness
				interpolatedOutput = startOutput + t * (endOutput - startOutput);
			}

			return interpolatedOutput;
		}

		/// <summary>
		/// Example quadratic ease-in-out function
		/// </summary>
		public static double EasingInOutFunction(double t)
		{
			return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
		}

		/// <summary>
		/// Example ease-out cubic function
		/// </summary>
		public static double EaseOutCubic(double t)
		{
			return 1.0 - Math.Pow(1.0 - t, 3.0);
		}
	}
}
