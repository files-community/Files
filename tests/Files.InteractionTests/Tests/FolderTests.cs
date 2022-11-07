using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Threading;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public class FolderTests
	{

		[TestCleanup]
		public void Cleanup()
		{
			var action = new Actions(SessionManager.Session);

			// Select the "Renamed Folder" folder and click the delete button
			TestHelper.InvokeButtonByName("Renamed Folder");
			TestHelper.InvokeButtonById("Delete");

			// Wait for prompt to show
			Thread.Sleep(1000);

			// Press the enter key
			action.SendKeys(Keys.Enter).Perform();

			// Select the "Renamed Folder - Copy"  folder and click the delete button
			TestHelper.InvokeButtonByName("Renamed Folder - Copy");
			TestHelper.InvokeButtonById("Delete");

			// Wait for prompt to show
			Thread.Sleep(1000);

			// Press the enter key
			action = new Actions(SessionManager.Session);
			action.SendKeys(Keys.Enter).Perform();

			// Wait for items to be deleted
			Thread.Sleep(1000);


			// Navigate back home
			TestHelper.InvokeButtonById("Home");
		}

		[TestMethod]
		public void TestFolders()
		{
			var action = new Actions(SessionManager.Session);

			// Navigation test

			// Click on the desktop item in the sidebar
			TestHelper.InvokeButtonById("Desktop");



			// Create folder test

			// User toolbar buttons to click the new folder option
			TestHelper.InvokeButtonById("InnerNavigationToolbarNewButton");
			TestHelper.InvokeButtonById("InnerNavigationToolbarNewFolderButton");

			// Check for axe issues in the prompt
			AxeHelper.AssertNoAccessibilityErrors();

			// Type the folder name
			action.SendKeys("New Folder").Perform();

			// Press the enter button to confirm
			action = new Actions(SessionManager.Session);
			action.SendKeys(Keys.Enter).Perform();

			// Wait for item to be created
			Thread.Sleep(1000);

			// Check for axe issues in the file area
			AxeHelper.AssertNoAccessibilityErrors();



			// Reanme folder test

			// Click on the rename button
			TestHelper.InvokeButtonById("InnerNavigationToolbarRenameButton");

			// Type the folder name
			action = new Actions(SessionManager.Session);
			action.SendKeys("Renamed Folder").Perform();

			// Press the enter button to save the new name
			action = new Actions(SessionManager.Session);
			action.SendKeys(Keys.Enter).Perform();

			// Wait for item to be renamed
			Thread.Sleep(1000);



			// Copy and paste folder test

			// Click on the copy button
			TestHelper.InvokeButtonById("InnerNavigationToolbarCopyButton");

			// Wait for item to be copied
			Thread.Sleep(1000);

			// Click on the paste button
			TestHelper.InvokeButtonById("InnerNavigationToolbarPasteButton");

			// Wait for item to be pasted
			Thread.Sleep(1000);
		}
	}
}
