namespace Files.InteractionTests.Tests
{
	[TestClass]
	public class NavigationsTests
	{

		[TestCleanup]
		public void Cleanup()
		{
			TestHelper.InvokeButtonById("Home");
		}

		[TestMethod]
		public void VerifyNavigationWorks()
		{
			TestHelper.InvokeButtonById("Desktop");
			AxeHelper.AssertNoAccessibilityErrors();
		}
	}
}
