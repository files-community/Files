// Copyright (c) Files Community
// Licensed under the MIT License.

using OpenQA.Selenium;
using System;
using System.Threading;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class FolderTests
	{
		[TestCleanup]
		public void Cleanup()
		{
			// Navigate back home
			TestHelper.InvokeButtonById("Home");
		}

		[TestMethod]
		public void TestFolders()
		{
			var id = System.DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
			var initialFolderName = $"IT New Folder {id}";
			var renamedFolderName = $"IT Renamed Folder {id}";

			NavigationTest();

			CreateFolderTest(initialFolderName);

			RenameFolderTest(initialFolderName, renamedFolderName);

			CopyPasteFolderTest(renamedFolderName);

			DeleteFolderTest(renamedFolderName);
		}

		/// <summary>
		/// Tests folder navigation
		/// </summary>
		private void NavigationTest()
		{
			// Navigate to the test data folder; all items this test creates live there
			TestHelper.NavigateToPath(TestHelper.TestDataRootPath);

			// Wait for the folder to settle
			Thread.Sleep(300);
		}


		/// <summary>
		/// Tests folder creation and checks for accessibility issues along the way
		/// </summary>
		private void CreateFolderTest(string folderName)
		{
			// Click the "New" button on the toolbar; the flyout item finder retries until it loads
			TestHelper.InvokeButtonById("InnerNavigationToolbarNewButton");

			// Click the "Folder" item from the menu flyout
			TestHelper.InvokeButtonById("InnerNavigationToolbarNewFolderButton");

			// Type the folder name into the dialog text box; this also waits for the dialog to open
			TestHelper.SetTextById("CreateItemDialogNameTextBox", folderName);

			// Check for accessibility issues in the new folder prompt
			AxeHelper.AssertNoAccessibilityErrors();

			// Click the "Create" button to confirm and wait for the dialog to close
			TestHelper.InvokeDialogPrimaryButton("Create");
			TestHelper.WaitUntilElementGoneById("CreateItemDialogNameTextBox");

			// Verify the folder shows up in the file area without clicking it,
			// since a click here and the selection click that follows could
			// land close enough together to register as a double click
			TestHelper.WaitForElementByName(folderName);

			// Check for accessibility issues in the file area
			AxeHelper.AssertNoAccessibilityErrors();
		}

		/// <summary>
		/// Tests renaming a folder
		/// </summary>
		private void RenameFolderTest(string currentFolderName, string renamedFolderName)
		{
			// Select the folder to avoid invoking Rename with a stale selection.
			TestHelper.InvokeButtonByName(currentFolderName);

			// Wait for the toolbar commands to enable for the new selection
			Thread.Sleep(500);

			// Click the "Rename" button on the toolbar
			TestHelper.InvokeButtonById("InnerNavigationToolbarRenameButton");

			// Type the new name into the inline text box and commit with Enter
			var renameBox = TestHelper.SetTextById("ItemNameTextBox", renamedFolderName);
			renameBox.SendKeys(Keys.Enter);

			// Verify the rename completed; this also re-selects the folder for the next step
			TestHelper.InvokeButtonByName(renamedFolderName);
		}

		/// <summary>
		/// Tests copying and pasting a folder
		/// </summary>
		private void CopyPasteFolderTest(string folderName)
		{
			// Click the "copy" button on the toolbar and give the clipboard a moment to settle
			TestHelper.InvokeButtonById("InnerNavigationToolbarCopyButton");
			Thread.Sleep(300);

			// Click the "paste" button on the toolbar
			TestHelper.InvokeButtonById("InnerNavigationToolbarPasteButton");

			// Wait for the pasted duplicate: a second item whose name contains the folder name
			// (the duplicate gets a localizable suffix, so the exact name is not predictable)
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
			while (TestHelper.GetElementsOfTypeWithContent("ListItem", folderName).Count < 2)
			{
				if (DateTime.UtcNow > deadline)
					Assert.Fail($"The pasted copy of '{folderName}' did not appear.");

				Thread.Sleep(300);
			}
		}

		/// <summary>
		/// Tests deleting folders
		/// </summary>
		private void DeleteFolderTest(string renamedFolderName)
		{
			// Select the "Renamed Folder" folder and clicks the "delete" button on the toolbar
			TestHelper.InvokeButtonByName(renamedFolderName);
			TestHelper.InvokeButtonById("InnerNavigationToolbarDeleteButton");

			// Wait for prompt to show
			Thread.Sleep(500);

			// Check for accessibility issues in the confirm delete prompt
			AxeHelper.AssertNoAccessibilityErrors();

			// Click the "Delete" button to confirm, then wait for the item to disappear
			TestHelper.InvokeDialogPrimaryButton("Delete");
			TestHelper.WaitUntilElementGoneByName(renamedFolderName);
		}
	}
}
