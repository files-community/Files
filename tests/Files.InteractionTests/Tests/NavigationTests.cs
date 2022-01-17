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
			TestHelper.InvokeButtonByName("Windows (C:)");
			AxeHelper.AssertNoAccessibilityErrors();

			var folderPaths = new string[]
			{
				"Windows",
				"System32"
			};

			foreach (var item in folderPaths)
			{
				for (int i = 0; i < 5; i++)
				{
					try
					{
						Console.WriteLine("Inoking item:" + item);
						Thread.Sleep(2000);
						TestHelper.InvokeButtonByName(item);
						i = 1000;
					}
					catch (Exception exc)
					{
						Console.WriteLine("Failed to invoke the item:" + item + " with exception" + exc.Message);
					}

				}
				try
				{
					// First run can be flaky due to external components
					AxeHelper.AssertNoAccessibilityErrors();
				}
				catch (System.Exception) { }
				AxeHelper.AssertNoAccessibilityErrors();
			}
		}
	}
}
