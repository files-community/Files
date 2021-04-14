using Files.Enums;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Files.Filesystem.Cloud.Providers
{
	public class AmazonDriveProvider : ICloudProviderDetector
	{
		public Task<IList<CloudProvider>> DetectAsync()
		{
			try
			{
				using var key = Registry.ClassesRoot.OpenSubKey(@"CLSID\{9B57F475-CCB0-4C85-88A9-2AA9A6C0809A}\Instance\InitPropertyBag");
				var syncedFolder = (string)key?.GetValue("TargetFolderPath");

				if (syncedFolder == null)
				{
					return Task.FromResult(Array.Empty<CloudProvider>() as IList<CloudProvider>);
				}

				return Task.FromResult(new CloudProvider[]
				{
					new CloudProvider()
					{
						ID = CloudProviders.AmazonDrive,
						Name = "Amazon Drive",
						SyncFolder = syncedFolder
					}
				} as IList<CloudProvider>);
			}
			catch
			{
				// Not detected
				return Task.FromResult(Array.Empty<CloudProvider>() as IList<CloudProvider>);
			}
		}
	}
}