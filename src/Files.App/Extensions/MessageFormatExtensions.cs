using Jeffijoe.MessageFormat;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Globalization;

namespace Files.App.Extensions
{
	/// <summary>
	/// Extension methods for working with localized resources and message formatting.
	/// </summary>
	public static class MessageFormatExtensions
	{
		/// <summary>
		/// Lazy initialization of resource map for accessing localized strings.
		/// </summary>
		private static readonly Lazy<ResourceMap> _resourcesTree = new(() => new ResourceManager().MainResourceMap.TryGetSubtree("Resources"));

		/// <summary>
		/// Lazy initialization of CultureInfo based on the application's primary language override.
		/// </summary>
		private static readonly Lazy<CultureInfo> _locale = new(() => new CultureInfo(AppLanguageHelper.PreferredLanguage.Code));

		/// <summary>
		/// Lazy initialization of custom value formatters for the message formatter.
		/// </summary>
		private static readonly Lazy<CustomValueFormatters> _customFormatter = new(() => new CustomValueFormatters
		{
			Number = (CultureInfo _, object? value, string? style, out string? formatted) =>
			{
				if (style is not null && style == string.Empty)
				{
					formatted = string.Format(CultureInfo.InvariantCulture, "{0:#,##0}", value);
					return true;
				}

				formatted = null;
				return false;
			}
		});

		/// <summary>
		/// Lazy initialization of message formatter with caching disabled.
		/// </summary>
		private static readonly Lazy<MessageFormatter> _formatter =new(() => new MessageFormatter(
			useCache: false,
			locale: _locale.Value.TwoLetterISOLanguageName,
			customValueFormatter: _customFormatter.Value));

		/// <summary>
		/// Creates a dictionary for format pairs with a string key.
		/// </summary>
		/// <param name="key">The key for the format pair.</param>
		/// <param name="value">The value for the format pair.</param>
		/// <returns>A dictionary containing the format pair.</returns>
		public static IReadOnlyDictionary<string, object?> ToFormatPairs(this string key, object value)
			=> new Dictionary<string, object?>(1) { [key] = value };

		/// <summary>
		/// Creates a dictionary for format pairs with an integer key.
		/// </summary>
		/// <param name="key">The key for the format pair.</param>
		/// <param name="value">The value for the format pair.</param>
		/// <returns>A dictionary containing the format pair.</returns>
		public static IReadOnlyDictionary<string, object?> ToFormatPairs(this int key, object value)
			=> new Dictionary<string, object?>(1) { [key.ToString(CultureInfo.InvariantCulture)] = value };

		/// <summary>
		/// Retrieves a localized resource string, formatting it with the provided pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <param name="pairs">The format pairs to use when formatting the resource string.</param>
		/// <returns>The formatted localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey, IReadOnlyDictionary<string, object?> pairs)
		{
			var value = _resourcesTree.Value?.TryGetValue(resourceKey)?.ValueAsString;

			if (value is null)
				return string.Empty;

			try
			{
				value = _formatter.Value.FormatMessage(value, pairs);
			}
			catch
			{
				value = string.Empty;
				App.Logger.LogWarning($"Formatter could not get a valid result value for: '{resourceKey}'");
			}

			return value;
		}

		/// <summary>
		/// Merges multiple dictionaries of format pairs and retrieves a localized resource string, formatting it with the merged pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <param name="pairs">An array of dictionaries containing the format pairs to use when formatting the resource string.</param>
		/// <returns>The formatted localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey, params IReadOnlyDictionary<string, object?>[] pairs)
		{
			var totalSize = pairs.Sum(dict => dict.Count);
			var mergedPairs = new Dictionary<string, object?>(totalSize);

			foreach (var dict in pairs)
				foreach (var pair in dict)
					mergedPairs[pair.Key] = pair.Value;

			return GetLocalizedFormatResource(resourceKey, mergedPairs);
		}

		/// <summary>
		/// Converts multiple values to a dictionary with their indices as keys and retrieves a localized resource string,
		/// formatting it with the created pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <param name="values">An array of values to use when formatting the resource string.</param>
		/// <returns>The formatted localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey, params object[] values)
		{
			var pairs = new Dictionary<string, object?>(values.Length);

			for (var i = 0; i < values.Length; i++)
				pairs[i.ToString(CultureInfo.InvariantCulture)] = values[i];

			return GetLocalizedFormatResource(resourceKey, pairs);
		}

		/// <summary>
		/// Retrieves a localized resource string without any format pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <returns>The localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey)
			=> GetLocalizedFormatResource(resourceKey, new Dictionary<string, object?>(1) { ["0"] = "" });
	}
}
