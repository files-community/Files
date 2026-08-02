using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Files.Shared.Extensions
{
	public static class GenericExtensions
	{
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TOut? TryCast<TOut>(this object? value, Func<TOut>? defaultValue = null)
		{
			if (value is TOut outValue)
				return outValue;

			return defaultValue is not null ? defaultValue() : default;
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
			where T : class
		{
			foreach (var item in source)
			{
				if (item is not null)
					yield return item;
			}
		}
	}
}
