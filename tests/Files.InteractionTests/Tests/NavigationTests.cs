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
			var action = new Actions(SessionManager.Session);
			action.SendKeys(OpenQA.Selenium.Keys.Escape).Build().Perform();
		}

		[TestMethod]
		public void VerifyNavigationWorks()
		{
			TestHelper.InvokeButtonByName("Desktop");
			AxeHelper.AssertNoAccessibilityErrors();
		}
	}
}
