// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Jeffijoe.MessageFormat;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Globalization;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Files.App.Extensions
{
	public static class MessageFormatExtensions
	{
		// Resource map for accessing localized strings
		private static readonly ResourceMap resourcesTree = new ResourceManager().MainResourceMap.TryGetSubtree("Resources");

		// CultureInfo based on the application's primary language override
		private static readonly CultureInfo locale = new(ApplicationLanguages.PrimaryLanguageOverride);

		// Message formatter with caching enabled, using the current UI culture's two-letter ISO language name
		private static readonly MessageFormatter formatter = new(useCache: true, locale: locale.TwoLetterISOLanguageName);

		// Extension method to create a dictionary for format pairs with a string key
		public static IReadOnlyDictionary<string, object?> ToFormatPairs(this string key, object value) => new Dictionary<string, object?> { [key] = value };

		// Extension method to create a dictionary for format pairs with an integer key
		public static IReadOnlyDictionary<string, object?> ToFormatPairs(this int key, object value) => new Dictionary<string, object?> { [key.ToString()] = value };

		// Retrieves a localized resource string, formatting it with the provided pairs
		public static string GetLocalizedFormatResource(this string resourceKey, IReadOnlyDictionary<string, object?> pairs)
		{
			var value = resourcesTree?.TryGetValue(resourceKey)?.ValueAsString;

			if (value is null)
				return string.Empty;

			try
			{
				value = formatter.FormatMessage(value, pairs);
			}
			catch
			{
				value = string.Empty;
				App.Logger.LogWarning($"Formatter could not get a valid result value for: '{resourceKey}'");
			}

			return value;
		}

		// Overloaded method to accept multiple dictionaries of pairs and merge them
		public static string GetLocalizedFormatResource(this string resourceKey, params IReadOnlyDictionary<string, object?>[] pairs)
		{
			var mergedPairs = pairs.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);
			return GetLocalizedFormatResource(resourceKey, mergedPairs);
		}

		// Overloaded method to accept multiple values and convert them to a dictionary with their indices as keys
		public static string GetLocalizedFormatResource(this string resourceKey, params object[] values)
		{
			var pairs = values.Select((value, index) => new KeyValuePair<string, object?>(index.ToString(), value))
							  .ToDictionary(pair => pair.Key, pair => pair.Value);
			return GetLocalizedFormatResource(resourceKey, pairs);
		}

		//TODO: Could replace `GetLocalizedResource()` in the future
		public static string GetLocalizedFormatResource(this string resourceKey) => GetLocalizedFormatResource(resourceKey, new Dictionary<string, object?>());
	}
}
