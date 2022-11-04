using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Threading;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public class CreateFolderTest
	{

		[TestCleanup]
		public void Cleanup()
		{
			var action = new Actions(SessionManager.Session);

			// Click the delete button in the toolbar
			TestHelper.InvokeButtonByName("Delete");

			// Wait for prompt to show
			Thread.Sleep(1000);

			// Press the enter key
			action.SendKeys(Keys.Enter).Build().Perform();

			// Wait for item to be deleted
			Thread.Sleep(1000);

			TestHelper.InvokeButtonById("Home");
		}

		[TestMethod]
		public void CreateFolder()
		{
			var action = new Actions(SessionManager.Session);

			// Navigate to desktop folder
			TestHelper.InvokeButtonById("Desktop"); 

			// User toolbar buttons to click the new folder option
			TestHelper.InvokeButtonByName("New");
			TestHelper.InvokeButtonByName("Folder");

			// Check for axe issues in the prompt
			AxeHelper.AssertNoAccessibilityErrors();

			// Type the folder name
			action.SendKeys("Test Folder").Build().Perform();

			// Click on the set name button
			TestHelper.InvokeButtonByName("Set Name");

			// Wait for item to be created
			Thread.Sleep(1000);

			// Check for axe issues in the file area
			AxeHelper.AssertNoAccessibilityErrors();
		}
	}
}
