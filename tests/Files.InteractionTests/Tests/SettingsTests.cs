using OpenQA.Selenium.Interactions;

namespace Files.InteractionTests.Tests
{
    [TestClass]
    public class SettingsTests
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
            AxeHelper.AssertNoAccessibilityErrors();

            var settingsItems = new string[]
            {
                "SettingsItemAppearance",
                "SettingsItemPreferences",
                "SettingsItemMultitasking",
                "SettingsItemExperimental",
                "SettingsItemAbout"
            };

            foreach (var item in settingsItems)
            {
                TestHelper.InvokeButtonById(item);
                AxeHelper.AssertNoAccessibilityErrors();
            }
        }
    }
}
