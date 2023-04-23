// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public class GeneralAppTests
	{
		[TestMethod]
		public void SessionGetsInitialized()
		{
			Assert.IsNotNull(SessionManager.Session);
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