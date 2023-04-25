// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;

namespace Files.App.Filesystem.StorageItems
{
	public class BaseBasicStorageItemExtraProperties : BaseStorageItemExtraProperties
	{
		private readonly IStorageItem item;

		public BaseBasicStorageItemExtraProperties(IStorageItem item) => this.item = item;

		public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
		{
			return AsyncInfo.Run<IDictionary<string, object>>(async (cancellationToken) =>
			{
				var props = new Dictionary<string, object>();
				propertiesToRetrieve.ForEach(x => props[x] = null);
				// Fill common poperties
				var ret = item.AsBaseStorageFile()?.GetBasicPropertiesAsync() ?? item.AsBaseStorageFolder()?.GetBasicPropertiesAsync();
				var basicProps = ret is not null ? await ret : null;
				props["System.ItemPathDisplay"] = item?.Path;
				props["System.DateCreated"] = basicProps?.ItemDate;
				props["System.DateModified"] = basicProps?.DateModified;
				return props;
			});
		}
	}
}
