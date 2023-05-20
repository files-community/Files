// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;

namespace Files.App.Filesystem.StorageItems
{
	public class BaseBasicStorageItemExtraProperties : BaseStorageItemExtraProperties
	{
		private readonly IStorageItem _item;

		public BaseBasicStorageItemExtraProperties(IStorageItem item)
		{
			_item = item;
		}

		public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
		{
			return AsyncInfo.Run<IDictionary<string, object>>(async (cancellationToken) =>
			{
				var props = new Dictionary<string, object>();

				propertiesToRetrieve.ForEach(x => props[x] = null);

				// Fill out common properties
				var ret = _item.AsBaseStorageFile()?.GetBasicPropertiesAsync() ?? _item.AsBaseStorageFolder()?.GetBasicPropertiesAsync();

				var basicProps = ret is not null ? await ret : null;

				props["System.ItemPathDisplay"] = _item?.Path;
				props["System.DateCreated"] = basicProps?.ItemDate;
				props["System.DateModified"] = basicProps?.DateModified;
				return props;
			});
		}
	}
}
