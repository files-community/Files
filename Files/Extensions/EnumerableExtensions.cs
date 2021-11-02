using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Files.Extensions
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Creates <see cref="List{T}"/> and returns <see cref="IEnumerable{T}"/> with provided <paramref name="item"/>
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <param name="item">The item</param>
        /// <returns><see cref="IEnumerable{T}"/> with <paramref name="item"/></returns>
        internal static IEnumerable<T> CreateEnumerable<T>(this T item) =>
            CreateList<T>(item);

        internal static List<T> CreateList<T>(this T item) =>
            new List<T>() { item };

        /// <summary>
        /// Executes given lambda parallelly on given data set with max degree of parallelism set up
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <param name="source">Data to process</param>
        /// <param name="body">Lambda to execute on all items</param>
        /// <param name="maxDegreeOfParallelism">Max degree of parallelism (-1 for unbounded execution)</param>
        /// <returns></returns>
        internal static Task AsyncParallelForEach<T>(this IEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var block = new ActionBlock<T>(body, options);

            foreach (var item in source)
                block.Post(item);

            block.Complete();
            return block.Completion;
        }

        public static bool AddIfNotPresent<T>(this IList<T> list, T element)
        {
            if (!list.Contains(element))
            {
                list.Add(element);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool AddIfNotPresent<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}