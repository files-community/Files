using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Extensions
{
    internal static class ThreadingExtensions
    {
        internal static async Task ExecuteParallel<T>
              (this IEnumerable<T> items, int limit, Func<T, Task> action)
        {
            List<Task> allTasks = new List<Task>(); // Store all Tasks
            List<Task> activeTasks = new List<Task>();

            foreach (T item in items)
            {
                if (activeTasks.Count >= limit)
                {
                    Task completedTask = await Task.WhenAny(activeTasks);
                    activeTasks.Remove(completedTask);
                }
                Task task = action(item);
                allTasks.Add(task);
                activeTasks.Add(task);
            }
            await Task.WhenAll(allTasks); // Wait for all task to complete
        }

        internal static async Task<IEnumerable<TResult>> Throttle<TResult>(this Queue<Func<Task<TResult>>> toRun, int throttleTo)
        {
            List<Task<TResult>> running = new List<Task<TResult>>(throttleTo);
            List<Task<TResult>> completed = new List<Task<TResult>>(toRun.Count());

            for (int i = 0; i < toRun.Count; i++)
            {
                Func<Task<TResult>> taskToRun = toRun.Dequeue();
                running.Add(taskToRun());
                if (running.Count == throttleTo)
                {
                    Task<TResult> comTask = await Task.WhenAny(running);
                    running.Remove(comTask);
                    completed.Add(comTask);
                }
            }
            
            return completed.Select(t => t.Result);
        }
    }
}
