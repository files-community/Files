// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Files.Shared.Extensions
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Creates <see cref="List{T}"/> and returns <see cref="IEnumerable{T}"/> with provided <paramref name="item"/>
		/// </summary>
		/// <typeparam name="T">The item type</typeparam>
		/// <param name="item">The item</param>
		/// <returns><see cref="IEnumerable{T}"/> with <paramref name="item"/></returns>
		public static IEnumerable<T> CreateEnumerable<T>(this T item)
		{
			return [item];
		}

		public static List<T> CreateList<T>(this T item)
		{
			return [item];
		}

		public static IList<T> AddIfNotPresent<T>(this IList<T> list, T element)
		{
			if (!list.Contains(element))
				list.Add(element);

			return list;
		}

		/// <summary>
		/// Executes given lambda parallelly on given data set with max degree of parallelism set up
		/// </summary>
		/// <typeparam name="T">The item type</typeparam>
		/// <param name="source">Data to process</param>
		/// <param name="body">Lambda to execute on all items</param>
		/// <param name="maxDegreeOfParallelism">Max degree of parallelism (-1 for unbounded execution)</param>
		/// <returns></returns>
		/// <param name="cts">Cancellation token, stops all remaining operations</param>
		/// <param name="scheduler">Task scheduler on which to execute `body`</param>
		/// <returns></returns>
		public static async Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, CancellationToken cts = default, TaskScheduler? scheduler = null)
		{
			var options = new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = maxDegreeOfParallelism,
				CancellationToken = cts
			};

			if (scheduler is not null)
				options.TaskScheduler = scheduler;

			var block = new ActionBlock<T>(body, options);

			foreach (var item in source)
				block.Post(item);

			block.Complete();

			await block.Completion;
		}

		public static Task<IList<T>> ToListAsync<T>(this IEnumerable<T> source)
		{
			return Task.Run(() => (IList<T>)source.ToList());
		}
	}
}
