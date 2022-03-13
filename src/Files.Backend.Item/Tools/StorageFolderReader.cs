using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Backend.Item.Tools
{
    internal static class StorageFolderReader
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(5);

        public static async Task<object> GetPropertyAsync(this StorageFolder folder, string key)
            => await GetPropertiesAsync(folder, key);
        public static async Task<T?> GetPropertyAsync<T>(this StorageFolder folder, string key)
        {
            var value = await GetPropertyAsync<T>(folder, key);
            return Cast<T>(value);
        }

        public static async Task<IDictionary<string, object>> GetPropertiesAsync(this StorageFolder folder, params string[] keys)
            => await folder.Properties.RetrievePropertiesAsync(keys).AsTask().WithTimeoutAsync(timeout) ?? new Dictionary<string, object>();
        public static async Task<IDictionary<string, T?>> GetPropertiesAsync<T>(this StorageFolder folder, params string[] keys)
        {
            var properties = await GetPropertiesAsync(folder, keys);
            return keys.ToDictionary(key => key, key => Cast<T>(properties[key]));
        }

        private static T? Cast<T>(object? o) => o is T value ? value : default;
    }
}
