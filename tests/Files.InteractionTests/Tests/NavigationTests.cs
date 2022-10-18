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
			TestHelpers.InvokeButtonById("Home");
		}

		[TestMethod]
		public void VerifyNavigationWorks()
		{
			TestHelpers.InvokeButtonById("Desktop");
			AxeHelper.AssertNoAccessibilityErrors();
		}
	}
}
