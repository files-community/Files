// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace App1
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			Assert.AreEqual(0, 0);
		}

		[UITestMethod]
		public void TestMethod2()
		{
			var grid = new Grid();
			Assert.AreEqual(0, grid.MinWidth);
		}
	}
}
