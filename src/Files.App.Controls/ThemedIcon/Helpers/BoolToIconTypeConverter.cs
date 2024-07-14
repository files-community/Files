// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;
using System;

namespace Files.App.Controls.Helpers
{
    /// <summary>
    /// Converts between Boolean and <see cref="ThemedIconTypes"/> for <see cref="ThemedIcon"/> selection binding.
    /// </summary>

    // Convert to and From ThemedIconTypes.Layered
    public class BoolToIconTypeLayeredConverter : IValueConverter
    {
        /// <summary>
        /// Converts true values to <see cref="ThemedIconTypes.Filled"/> and false values to
        /// <see cref="ThemedIconTypes.Layered"/>. If the parameter is "Invert", we do the opposite conversion.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var inverse = (parameter != null && string.Compare((string)parameter, "Invert", StringComparison.OrdinalIgnoreCase) == 0);

            if (inverse)
            {
                if (value is bool && (bool)value)
                {
                    return ThemedIconTypes.Layered;
                }

                return ThemedIconTypes.Filled;
            }
            else
            {
                if (value is bool && (bool)value)
                {
                    return ThemedIconTypes.Filled;
                }

                return ThemedIconTypes.Layered;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var inverse = (parameter != null && string.Compare((string)parameter, "Invert", StringComparison.OrdinalIgnoreCase) == 0);

            if (inverse)
            {
                return (value is ThemedIconTypes && (ThemedIconTypes)value == ThemedIconTypes.Layered);
            }
            else
            {
                return (value is ThemedIconTypes && (ThemedIconTypes)value == ThemedIconTypes.Filled);
            }
        }
    }

    // Convert to and From ThemedIconTypes.Outline
    public class BoolToIconTypeOutlineConverter : IValueConverter
    {
        /// <summary>
        /// Converts true values to <see cref="ThemedIconTypes.Filled"/> and false values to
        /// <see cref="ThemedIconTypes.Outline"/>. If the parameter is "Invert", we do the opposite conversion.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var inverse = (parameter != null && string.Compare((string)parameter, "Invert", StringComparison.OrdinalIgnoreCase) == 0);

            if (inverse)
            {
                if (value is bool && (bool)value)
                {
                    return ThemedIconTypes.Outline;
                }

                return ThemedIconTypes.Filled;
            }
            else
            {
                if (value is bool && (bool)value)
                {
                    return ThemedIconTypes.Filled;
                }

                return ThemedIconTypes.Outline;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var inverse = (parameter != null && string.Compare((string)parameter, "Invert", StringComparison.OrdinalIgnoreCase) == 0);

            if (inverse)
            {
                return (value is ThemedIconTypes && (ThemedIconTypes)value == ThemedIconTypes.Outline);
            }
            else
            {
                return (value is ThemedIconTypes && (ThemedIconTypes)value == ThemedIconTypes.Filled);
            }
        }
    }
}