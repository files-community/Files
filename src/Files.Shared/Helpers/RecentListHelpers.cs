// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.Shared.Helpers
{
	public static class RecentListHelpers
	{
		public static List<T> AddOrMoveToFront<T, TKey>(IEnumerable<T>? items, T item, Func<T, TKey> keySelector, int maxCount, IEqualityComparer<TKey>? comparer = null)
		{
			return CollapseAndTrim([item, .. items ?? []], keySelector, maxCount, comparer);
		}

		public static List<T> CollapseAndTrim<T, TKey>(IEnumerable<T>? items, Func<T, TKey> keySelector, int maxCount, IEqualityComparer<TKey>? comparer = null)
		{
			if (maxCount <= 0 || items is null)
				return [];

			HashSet<TKey> seen = new(comparer);
			List<T> result = [];

			foreach (var item in items)
			{
				if (!seen.Add(keySelector(item)))
					continue;

				result.Add(item);
				if (result.Count == maxCount)
					break;
			}

			return result;
		}
	}
}
