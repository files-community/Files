// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Storage.NativeStorage;
using Files.Backend.Models;
using Files.Backend.Services;

namespace Files.App.ServicesImplementation
{
	public class RecycleBinService : ITrashService
	{
		private readonly NativeStorageService storageService;

		public RecycleBinService(NativeStorageService storageService)
		{
			this.storageService = storageService;
		}

		public ITrashWatcher CreateWatcher()
		{
			return new RecycleBinWatcher(storageService);
		}
	}
}
