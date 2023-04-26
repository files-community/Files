// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage.FileProperties;

namespace Files.App.Filesystem.StorageItems
{
	public class BaseStorageItemExtraProperties : IStorageItemExtraProperties
	{
		public virtual IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
			=> AsyncInfo.Run((cancellationToken) =>
			{
				var props = new Dictionary<string, object>();
				propertiesToRetrieve.ForEach(x => props[x] = null);
				return Task.FromResult<IDictionary<string, object>>(props);
			});

		public virtual IAsyncAction SavePropertiesAsync()
			=> Task.CompletedTask.AsAsyncAction();
		public virtual IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
			=> Task.CompletedTask.AsAsyncAction();
	}
}
