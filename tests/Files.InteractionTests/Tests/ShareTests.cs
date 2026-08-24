// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Files.InteractionTests.Tests
{
	// The toolbar Share command drives ShareItemHelpers.ShareItemsAsync, which crashed under Native AOT.
	[TestClass]
	public sealed class ShareTests
	{
		private static readonly string testFolderName = $"IT ShareTests {DateTime.UtcNow:yyyyMMddHHmmssfff}";
		private static readonly string testFolderPath = Path.Combine(TestHelper.TestDataRootPath, testFolderName);

		[ClassCleanup]
		public static void ClassCleanup()
		{
			if (!Directory.Exists(testFolderPath))
				return;

			// Leave the folder before deleting it so the file area is not left on a dead path
			TestHelper.InvokeButtonById("Home");

			try
			{
				Directory.Delete(testFolderPath, true);
			}
			catch (IOException)
			{
				// The app can briefly keep a change-watcher handle on the folder right after navigating away
			}
		}

		[TestCleanup]
		public void Cleanup()
		{
			TestHelper.SendEscKey();
		}

		[TestMethod]
		public void TestShareFromToolbar()
		{
			var fileName = CreateTestFile();

			// Select the file so the toolbar's Share command becomes executable
			TestHelper.InvokeButtonByName(fileName);
			TestHelper.InvokeButtonById("InnerNavigationToolbarShareButton");

			Assert.IsTrue(TryCloseShareSheet(), "The share window did not open after invoking Share.");

			Assert.IsTrue(TestHelper.ElementExistsByName(fileName, 10000),
				"Files crashed or stopped responding after invoking Share.");
		}

		private static string CreateTestFile()
		{
			Directory.CreateDirectory(testFolderPath);
			TestHelper.NavigateToPath(testFolderPath);

			var fileName = $"Share {DateTime.UtcNow:yyyyMMddHHmmssfff}.txt";

			TestHelper.InvokeButtonById("InnerNavigationToolbarNewButton");
			TestHelper.InvokeButtonById("File");
			TestHelper.SetTextById("CreateItemDialogNameTextBox", fileName);
			TestHelper.InvokeDialogPrimaryButton("Create");

			// Wait for the dialog to fully close before touching the new item (its name matches the text box)
			TestHelper.WaitUntilElementGoneById("CreateItemDialogNameTextBox");
			TestHelper.WaitForElementByName(fileName);

			return fileName;
		}

		private const uint WM_CLOSE = 0x0010;

		private const string FilesWindowClass = "WinUIDesktopWin32WindowClass";

		// The share sheet is out-of-process, so it can't be seen via the app's UIA session; instead it's
		// detected as the foreground window (any window other than Files) and dismissed with WM_CLOSE.
		private static bool TryCloseShareSheet()
		{
			HWND shareSheet = default;
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
			while (DateTime.UtcNow < deadline)
			{
				var foreground = PInvoke.GetForegroundWindow();
				var classNameBuffer = new char[256];
				int classNameLength = PInvoke.GetClassName(foreground, classNameBuffer);

				if (!foreground.IsNull && classNameLength > 0 && new string(classNameBuffer, 0, classNameLength) != FilesWindowClass)
				{
					shareSheet = foreground;
					break;
				}

				Thread.Sleep(200);
			}

			if (shareSheet.IsNull)
				return false;

			// Let the sheet finish opening before dismissing it, otherwise WM_CLOSE cancels it mid-open
			Thread.Sleep(700);
			PInvoke.PostMessage(shareSheet, WM_CLOSE, default, default);
			return true;
		}
	}
}
