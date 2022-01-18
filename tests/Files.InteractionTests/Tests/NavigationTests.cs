using System;
using System.Threading;
using OpenQA.Selenium.Interactions;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public class NavigationsTests
	{

		[TestCleanup]
		public void Cleanup()
		{
			TestHelper.InvokeButtonByName("Home");
		}

		[TestMethod]
		public void VerifyNavigationWorks()
		{
			TestHelper.InvokeButtonByName("Desktop");
			AxeHelper.AssertNoAccessibilityErrors();
		}
	}
}
