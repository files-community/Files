// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.UnitTests
{
	[TestClass]
	public class WindowsStorageTests
	{
		[TestMethod]
		public void Test_WindowsStorageTests_GetShellNewItems()
		{
			var folder = WindowsStorable.TryParse("C:\\Windows") as WindowsFolder;

			Assert.IsNotNull(folder);

			var items = folder.GetShellNewMenuItems();

			Assert.IsNotNull(items);

			foreach (var item in items)
			{
				Assert.IsNotNull(item.Name);

				if (item.State is not WindowsContextMenuState.Disabled && item.Type is WindowsContextMenuType.Bitmap)
					Assert.IsNotNull(item.Icon);
			}
		}

		[TestMethod]
		public void Test_WindowsStorageTests_GetOpenWithMenuItems()
		{
			var file = WindowsStorable.TryParse("C:\\Windows\\system.ini") as WindowsFile;

			Assert.IsNotNull(file);

			var items = file.GetOpenWithMenuItems();

			Assert.IsNotNull(items);

			foreach (var item in items)
			{
				Assert.IsNotNull(item.Name);

				if (item.State is not WindowsContextMenuState.Disabled && item.Type is WindowsContextMenuType.Bitmap)
					Assert.IsNotNull(item.Icon);
			}
		}
	}
}
