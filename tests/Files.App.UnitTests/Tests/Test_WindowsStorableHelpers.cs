// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;

namespace Files.App.UnitTests
{
	[STATestClass]
	public unsafe class Test_WindowsStorableHelpers
	{
		[STATestMethod]
		public void Test_GetShellNewItems()
		{
			using var folder = WindowsStorable.TryParse(FOLDERID.FOLDERID_Desktop) as WindowsFolder;
			Assert.IsNotNull(folder, $"\"{nameof(folder)}\" must not be null.");

			var items = folder.GetShellNewMenuItems();
			Assert.IsNotNull(items, $"\"{nameof(items)}\" must not be null.");

			foreach (var item in items)
			{
				Assert.IsNotNull(item.Name);

				if (item.State is not WindowsContextMenuState.Disabled && item.Type is WindowsContextMenuType.Bitmap)
					Assert.IsNotNull(item.Icon);
			}
		}

		[STATestMethod]
		public void Test_GetOpenWithMenuItems()
		{
			PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED);

			HRESULT hr = default;
			using var bulkOperations = new WindowsBulkOperations();
			using var desktopFolder = WindowsStorable.TryParse(FOLDERID.FOLDERID_Desktop) as WindowsFolder;
			Assert.IsNotNull(desktopFolder, $"\"{nameof(desktopFolder)}\" was null.");
			hr = bulkOperations.QueueCreateOperation(desktopFolder, 0, "Test_GetOpenWithMenuItems.txt", null);
			Assert.IsTrue(hr.Succeeded, $"Failed to queue the create operation: {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the copy operation: {hr}");

			using var file = WindowsStorable.TryParse("::{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}\\Test_GetOpenWithMenuItems.txt") as WindowsFile;
			Assert.IsNotNull(file, $"\"{nameof(file)}\" must not be null.");

			var items = file.GetOpenWithMenuItems();
			Assert.IsNotNull(items, $"\"{nameof(items)}\" must not be null.");

			foreach (var item in items)
			{
				Assert.IsNotNull(item.Name);

				if (item.State is not WindowsContextMenuState.Disabled && item.Type is WindowsContextMenuType.Bitmap)
					Assert.IsNotNull(item.Icon);
			}

			hr = bulkOperations.QueueDeleteOperation(file);
			Assert.IsTrue(hr.Succeeded, $"Failed to queue delete operation for \"{nameof(file)}\": {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the delete operation: {hr}");
		}
	}
}
