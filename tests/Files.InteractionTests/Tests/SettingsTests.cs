// Copyright (c) Files Community
// Licensed under the MIT License.

using OpenQA.Selenium.Interactions;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class SettingsTests
	{

		[TestCleanup]
		public void Cleanup()
		{
			var action = new Actions(SessionManager.Session);
			action.SendKeys(OpenQA.Selenium.Keys.Escape).Build().Perform();
		}

		[TestMethod]
		public void VerifySettingsAreAccessible()
		{
			TestHelper.InvokeButtonById("SettingsButton");
			AxeHelper.AssertNoAccessibilityErrors(error =>
				AxeHelper.IsCommunityToolkitWindowsIssue430(error) ||
				AxeHelper.IsCommunityToolkitSettingsCardButtonNameIssue(error));

			var settingsItems = new string[]
			{
				"SettingsItemGeneral",
				"SettingsItemAppearance",
				"SettingsItemLayout",
				"SettingsItemFolders",
				"SettingsItemActions",
				"SettingsItemTags",
				"SettingsItemDevTools",
				"SettingsItemAdvanced",
				"SettingsItemAbout"
			};

			foreach (var item in settingsItems)
			{
				for (int i = 0; i < 5; i++)
				{
					try
					{
						System.Console.WriteLine("Invoking button:" + item);
						System.Threading.Thread.Sleep(3000);
						TestHelper.InvokeButtonById(item);
						i = 1000;
					}
					catch (System.Exception exc)
					{
						System.Console.WriteLine("Failed to invoke the button:" + item + " with exception" + exc.Message);
					}

				}
				try
				{
					// First run can be flaky due to external components
					AxeHelper.AssertNoAccessibilityErrors(error =>
						AxeHelper.IsCommunityToolkitWindowsIssue430(error) ||
						AxeHelper.IsCommunityToolkitSettingsCardButtonNameIssue(error));
				}
				catch (System.Exception) { }
				AxeHelper.AssertNoAccessibilityErrors(error =>
					AxeHelper.IsCommunityToolkitWindowsIssue430(error) ||
					AxeHelper.IsCommunityToolkitSettingsCardButtonNameIssue(error));
			}
		}
	}
}
