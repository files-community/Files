using System;
using System.Threading.Tasks;

namespace Files.Shared.Extensions
{
    public static class TaskExtensions
    {
        public static async Task WithTimeoutAsync(this Task task, TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
            {
                await task;
            }
        }

        public static async Task<T?> WithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, T? defaultValue = default)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
            {
                return await task;
            }
            return defaultValue;
        }
    }
}
