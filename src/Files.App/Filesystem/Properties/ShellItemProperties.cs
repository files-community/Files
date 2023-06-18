using Files.App.Helpers;
using Files.App.Shell;
using Files.Sdk.Storage.LocatableStorage;
using Microsoft.UI.Xaml.Media.Imaging;
using Vanara.Windows.Shell;
using Windows.Storage.Streams;
using static Vanara.PInvoke.Ole32;

namespace Files.App.Filesystem.Properties
{
	public class ShellItemProperties : IStorageProperties
	{
		private readonly ShellItem item;

		public DateTime DateCreated => item.Properties.GetProperty<DateTime>(PROPERTYKEY.System.DateCreated);

		public DateTime DateModified => item.Properties.GetProperty<DateTime>(PROPERTYKEY.System.DateModified);

		public ulong? Size => item.Properties.GetProperty<ulong>(PROPERTYKEY.System.Size);

		public ShellItemProperties(ILocatableStorable item)
		{
			this.item = new ShellItem(item.Path);
		}

		public async IAsyncEnumerable<KeyValuePair<string, object>> GetStoragePropertiesAsync(ItemPropertiesKind propertiesKind = ItemPropertiesKind.Standard, CancellationToken cancellationToken = default)
		{
			var propertyStore = item.Properties;
			propertyStore.IncludeSlow = (propertiesKind == ItemPropertiesKind.Extended) ? true : false;
			foreach (KeyValuePair<PROPERTYKEY, object> pair in propertyStore.AsEnumerable())
			{
				yield return new KeyValuePair<string, object>(pair.Key.GetCanonicalName(), pair.Value);
			}
		}

		public Task<KeyValuePair<string, object>> GetStoragePropertyAsync(string canonicalName)
		{
			if (item.Properties.FirstOrDefault(x => x.Key.GetCanonicalName().Equals(canonicalName)) is object o)
			{
				return Task.FromResult(new KeyValuePair<string, object>(canonicalName, o));
			}
			else
			{
				return Task.FromException<KeyValuePair<string, object>>(new ArgumentException($"A property value which corresponds to the canonical name {canonicalName} was not found."));
			}
		}

		public Task<IRandomAccessStream> GetThumbnailAsync(uint requestedSize)
		{
			return item.GetThumbnailAsync(requestedSize);
		}

		public Task<IRandomAccessStream?> GetOverlayIconAsync()
		{
			return item.GetOverlayIconAsync();
		}

		public async IAsyncEnumerable<KeyValuePair<string, object>> RetrievePropertiesAsync(string[] strings)
		{
			foreach (var key in strings)
			{
				yield return await GetStoragePropertyAsync(key);
			}
		}
	}
}
