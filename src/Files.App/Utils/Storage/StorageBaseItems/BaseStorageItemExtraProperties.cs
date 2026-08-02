// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public partial class BaseStorageItemExtraProperties : IStorageItemExtraProperties
	{
		public virtual IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
		{
			return
				AsyncInfo.Run((cancellationToken) =>
				{
					var props = new Dictionary<string, object>();
					propertiesToRetrieve.ForEach(x => props.SetNullablePropertyValue(x, null));

					return Task.FromResult<IDictionary<string, object>>(props);
				});
		}

		public virtual IAsyncAction SavePropertiesAsync()
		{
			return Task.CompletedTask.AsAsyncAction();
		}

		public virtual IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
		{
			return Task.CompletedTask.AsAsyncAction();
		}

	}

	public static class StorageItemExtraPropertiesExtensions
	{
		public static IAsyncAction SaveNullablePropertiesAsync(
			this IStorageItemExtraProperties properties,
			IEnumerable<KeyValuePair<string, object?>> propertiesToSave)
		{
			return properties.SavePropertiesAsync(propertiesToSave.Select(
				static property => new KeyValuePair<string, object>(property.Key, ToWinRtPropertyValue(property.Value))));
		}

		internal static void SetNullablePropertyValue(
			this IDictionary<string, object> properties,
			string name,
			object? value)
		{
			properties[name] = ToWinRtPropertyValue(value);
		}

		// WinRT property bags use null for unavailable values despite the projected object contract.
		private static object ToWinRtPropertyValue(object? value)
			=> value!;
	}
}
