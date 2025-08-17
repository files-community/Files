// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed partial class GenericEnumConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return ConvertInternal(value, targetType, parameter, language,
				s => ParseEnumConversionString(s, false));
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return ConvertInternal(value, targetType, parameter, language,
				s => ParseEnumConversionString(s, true));
		}

		private static Dictionary<long, long> ParseEnumConversionString(string input, bool reverseKeyValue)
		{
			var result = new Dictionary<long, long>();

			if (string.IsNullOrWhiteSpace(input))
				return result;

			try
			{
				var segments = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
				
				foreach (var segment in segments)
				{
					var trimmedSegment = segment.Trim();
					if (string.IsNullOrEmpty(trimmedSegment))
						continue;

					var parts = trimmedSegment.Split('-', StringSplitOptions.RemoveEmptyEntries);
					
					// Ensure we have exactly 2 parts
					if (parts.Length != 2)
						continue;

					// Try to parse both parts as long values
					if (long.TryParse(parts[0].Trim(), out var first) && 
					    long.TryParse(parts[1].Trim(), out var second))
					{
						if (reverseKeyValue)
						{
							// For ConvertBack, use second value as key and first as value
							if (!result.ContainsKey(second))
								result.Add(second, first);
						}
						else
						{
							// For Convert, use first value as key and second as value
							if (!result.ContainsKey(first))
								result.Add(first, second);
						}
					}
				}
			}
			catch
			{
				// If any unexpected error occurs during parsing, return empty dictionary
				// This ensures the converter doesn't crash and can continue with default behavior
			}

			return result;
		}

		private object ConvertInternal(object value, Type targetType, object parameter, string language, Func<string, Dictionary<long, long>> enumConversion)
		{
			var enumValue = System.Convert.ToInt64(value);

			if (parameter is string strParam)
			{
				// enumValue-convertedValue: 0-1,1-2
				var enumConversionValues = enumConversion(strParam);

				if (enumConversionValues.TryGetValue(enumValue, out var convertedValue))
				{
					enumValue = convertedValue;
				}
				// else.. use value from the cast above
			}

			try
			{
				if (Enum.GetName(targetType, enumValue) is string enumName)
				{
					return Enum.Parse(targetType, enumName);
				}
			}
			catch { }

			try
			{
				return System.Convert.ChangeType(enumValue, targetType);
			}
			catch { }

			return enumValue;
		}
	}
}
