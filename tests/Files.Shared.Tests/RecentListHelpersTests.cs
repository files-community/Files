// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;

namespace Files.Shared.Tests;

[TestClass]
public class RecentListHelpersTests
{
	[TestMethod]
	public void AddOrMoveToFront_InsertsMostRecentItemFirst()
	{
		var result = RecentListHelpers.AddOrMoveToFront(
			["C:\\Work", "C:\\Users"],
			"C:\\Temp",
			path => path,
			5,
			StringComparer.OrdinalIgnoreCase);

		CollectionAssert.AreEqual(
			new List<string> { "C:\\Temp", "C:\\Work", "C:\\Users" },
			result);
	}

	[TestMethod]
	public void AddOrMoveToFront_MovesExistingItemToTopWithoutDuplication()
	{
		var result = RecentListHelpers.AddOrMoveToFront(
			["C:\\Work", "C:\\Users", "C:\\Temp"],
			"c:\\users",
			path => path,
			5,
			StringComparer.OrdinalIgnoreCase);

		CollectionAssert.AreEqual(
			new List<string> { "c:\\users", "C:\\Work", "C:\\Temp" },
			result);
	}

	[TestMethod]
	public void AddOrMoveToFront_EnforcesMaximumSize()
	{
		var result = RecentListHelpers.AddOrMoveToFront(
			["C:\\One", "C:\\Two", "C:\\Three"],
			"C:\\Four",
			path => path,
			3,
			StringComparer.OrdinalIgnoreCase);

		CollectionAssert.AreEqual(
			new List<string> { "C:\\Four", "C:\\One", "C:\\Two" },
			result);
	}

	[TestMethod]
	public void CollapseAndTrim_DeduplicatesPersistedItemsWhilePreservingOrder()
	{
		var result = RecentListHelpers.CollapseAndTrim(
			["C:\\One", "c:\\one", "C:\\Two", "C:\\Three", "C:\\Two"],
			path => path,
			3,
			StringComparer.OrdinalIgnoreCase);

		CollectionAssert.AreEqual(
			new List<string> { "C:\\One", "C:\\Two", "C:\\Three" },
			result);
	}
}
