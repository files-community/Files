using OpenQA.Selenium.Interactions;
using System;
using System.Threading;

namespace Files.InteractionTests.Tests
{
    [TestClass]
    public class ItemOperationTests
    {

        [TestCleanup]
        public void Cleanup()
        {

        }

        [TestMethod]
        public void VerifyItemOperations()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(2000);
                    NavigateToSidebarItem("Windows (C:)");
                    i = 1000;
                }
                catch (Exception){}

            }

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(2000);
                    CreateFolder("New folder");
                    i = 1000;
                }
                catch (Exception) { }

            }

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(2000);
                    TestHelper.InvokeButtonByName("New folder"); // Focus on the newly created item
                    i = 1000;
                }
                catch (Exception) { }

            }

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(2000);
                    RenameItem("Folder");
                    i = 1000;
                }
                catch (Exception) { }

            }

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(2000);
                    TestHelper.InvokeButtonByName("Folder"); // Focus on the renamed item
                    i = 1000;
                }
                catch (Exception) { }

            }

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(2000);
                    DeleteItem("Folder");
                    i = 1000;
                }
                catch (Exception) { }

            }

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(2000);
                    NavigateToSidebarItem("Home"); // TODO fix issues in details layout so that settings dialog can be opened without triggering an issue
                    i = 1000;
                }
                catch (Exception) { }
            }
        }

        private void CreateFolder(string folderName)
        {
            TestHelper.InvokeButtonByName("New"); // Click the "new" button in the toolbar
            TestHelper.InvokeButtonByName("Folder"); // Click the "folder" menu item in the flyout
            var action = new Actions(SessionManager.Session);
            action.SendKeys(folderName).Build().Perform(); // Type the folder name
            TestHelper.InvokeButtonByName("Set Name"); // Click the "set" name button
        }

        private void RenameItem(string itemName)
        {
            var action = new Actions(SessionManager.Session);
            action.SendKeys(OpenQA.Selenium.Keys.F2).Build().Perform();
            action.SendKeys(itemName).Build().Perform(); // Type the new name
            action.SendKeys(OpenQA.Selenium.Keys.Enter).Build().Perform();
        }

        private void DeleteItem(string itemName)
        {
            var action = new Actions(SessionManager.Session);
            action.SendKeys(OpenQA.Selenium.Keys.Delete).Build().Perform();
            action.SendKeys(OpenQA.Selenium.Keys.Enter).Build().Perform();
        }

        private void NavigateToSidebarItem(string sidebarItem)
        {
            TestHelper.InvokeButtonByName(sidebarItem);
        }
    }
}
