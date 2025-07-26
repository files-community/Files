// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Storage;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;

namespace Files.App.UnitTests
{
	[TestClass]
	public class WindowsStorageTests
	{
		[TestMethod]
		public void Test_WindowsStorageTests_GetOpenWithMenuItems()
		{
			var file = WindowsStorable.TryParse("C:\\Windows\\system.ini") as WindowsFile;

			Assert.IsNotNull(file);

			var items = file.GetOpenWithMenuItems();

			Assert.AreNotEqual(0, items.Count());
		}
	}
}
