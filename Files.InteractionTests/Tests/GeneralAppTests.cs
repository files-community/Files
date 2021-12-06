using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.InteractionTests.Tests
{
    [TestClass]
    public class GeneralAppTests
    {
        [TestMethod]
        public void SessionGetsInitialized()
        {
            Assert.IsNotNull(TestRunInitializer.Session);
            TestHelper.VerifyNoAccessibilityErrors();
        }

        [TestMethod]
        public void SettingsAreAccessible()
        {
            TestHelper.InvokeButton("Settings");
            TestHelper.VerifyNoAccessibilityErrors();

            TestHelper.InvokeButton("Preferences");
            TestHelper.VerifyNoAccessibilityErrors();

            TestHelper.InvokeButton("Experimental");
            TestHelper.VerifyNoAccessibilityErrors();
        }
    }
}
