namespace Files.InteractionTests.Tests
{
    [TestClass]
    public class GeneralAppTests
    {
        [TestMethod]
        public void SessionGetsInitialized()
        {
            Assert.IsNotNull(SessionManager.Session);
            AxeHelper.AssertNoAccessibilityErrors();
        }
    }
}
