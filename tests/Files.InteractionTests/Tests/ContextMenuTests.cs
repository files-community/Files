// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.IO;
using System.Threading;

namespace Files.InteractionTests.Tests
{
	// Tests are declared in the order the app naturally flows: the Home page (where the app opens)
	// first, then the sidebar, then the file area. File-area tests share a single folder under the
	// test data root, created lazily on first use and deleted afterwards, so no user folder is
	// ever littered with test items.
	[TestClass]
	public sealed class ContextMenuTests
	{
		private static readonly string testFolderName = $"IT ContextMenuTests {DateTime.UtcNow:yyyyMMddHHmmssfff}";
		private static readonly string testFolderPath = Path.Combine(TestHelper.TestDataRootPath, testFolderName);

		// Tracks whether the file area currently shows the shared test folder, so tests skip the
		// navigation when a previous test already left it there
		private static bool isInTestFolder;

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
				// The app can briefly keep a change-watcher handle on the folder right after
				// navigating away; the assembly cleanup removes the whole root once the app closed
			}
		}

		[TestCleanup]
		public void Cleanup()
		{
			// Close any menu a failed assertion may have left open. Navigation state is left as-is;
			// each test navigates only when it needs a different page.
			TestHelper.SendEscKey();
		}

		/// <summary>
		/// Tests the context menus of the Home page widget cards: the first pinned-item card with
		/// every submenu expanded, then the Recycle Bin card, then a drive card. Runs first because
		/// Home is the page the app opens on.
		/// </summary>
		[TestMethod]
		public void TestWidgetContextMenus()
		{
			TestHelper.InvokeButtonById("Home");
			isInTestFolder = false;

			// First card of the pinned widget (the topmost widget on the page)
			var pinnedCard = TestHelper.GetFirstWidgetCard();
			Assert.IsNotNull(pinnedCard, "No pinned-item card was found on the Home page.");
			TestHelper.ContextClickElement(pinnedCard);
			TestHelper.WaitForElementByName("Open in new tab");
			AxeHelper.AssertNoAccessibilityErrors();

			// Expand every submenu the card menu offers
			ExpandSubMenuIfPresent("Send to", () => TestHelper.ContextClickElement(pinnedCard), allowLeafFallback: true);
			ExpandSubMenuIfPresent("Show more options", () => TestHelper.ContextClickElement(pinnedCard));
			CloseMenu();

			// Recycle Bin card (only present when pinned); its menu has no submenus to expand.
			// Unlike the sidebar, the card menu is the generic pinned-item menu, so "Empty Recycle
			// Bin" may not be offered - fall back to the generic menu marker.
			var recycleBinCard = TestHelper.GetWidgetCardByName("Recycle Bin");
			if (recycleBinCard is not null)
			{
				TestHelper.ContextClickElement(recycleBinCard);
				if (!TestHelper.ElementExistsByName("Empty Recycle Bin", 2000))
					TestHelper.WaitForElementByName("Open in new tab");
				AxeHelper.AssertNoAccessibilityErrors();
				CloseMenu();
			}

			// Drive card, with its submenu expanded as well
			var driveCard = TestHelper.GetWidgetCardByName("(C:)");
			if (driveCard is not null)
			{
				TestHelper.ContextClickElement(driveCard);
				TestHelper.WaitForElementByName("Open in new tab");
				AxeHelper.AssertNoAccessibilityErrors();
				ExpandSubMenuIfPresent("Show more options", () => TestHelper.ContextClickElement(driveCard));
				CloseMenu();
			}
		}

		/// <summary>
		/// Tests the sidebar context menus (pinned folder, Recycle Bin, drive). The sidebar is
		/// visible on every page, so no navigation is needed.
		/// </summary>
		[TestMethod]
		public void TestSidebarContextMenus()
		{
			// Pinned folder, with every submenu its menu offers expanded
			TestHelper.ContextClickElementById("Desktop");
			TestHelper.WaitForElementByName("Open in new tab");
			AxeHelper.AssertNoAccessibilityErrors();
			ExpandSubMenuIfPresent("Send to", () => TestHelper.ContextClickElementById("Desktop"), allowLeafFallback: true);
			ExpandSubMenuIfPresent("Show more options", () => TestHelper.ContextClickElementById("Desktop"));
			CloseMenu();

			// Drive from the Drives section, with every submenu expanded
			var driveItems = TestHelper.GetElementsOfTypeWithContent("ListItem", "(C:)");
			if (driveItems.Count > 0)
			{
				var driveItem = driveItems[0];
				TestHelper.ContextClickElement(driveItem);
				TestHelper.WaitForElementByName("Open in new tab");
				AxeHelper.AssertNoAccessibilityErrors();
				ExpandSubMenuIfPresent("Show more options", () => TestHelper.ContextClickElement(driveItem));
				CloseMenu();
			}
		}

		/// <summary>
		/// Tests the item context menu: core commands present, primary icon buttons exposed with
		/// accessible names, Show more options expandable, no accessibility errors while open, and
		/// keyboard invocation via Shift+F10.
		/// </summary>
		[TestMethod]
		public void TestItemContextMenu()
		{
			var folderName = $"Folder {DateTime.UtcNow:yyyyMMddHHmmssfff}";

			NavigateToTestFolder();
			CreateItemFromNewFlyout("InnerNavigationToolbarNewFolderButton", folderName);

			// Right-click the folder. The element finders retry, so no fixed wait is needed for
			// the menu itself or for the async shell items to land.
			TestHelper.ContextClickElementByName(folderName);

			// Core commands must be present. Cut/Copy/Rename/Delete/Properties render as the primary
			// icon-button row, so finding them by name also guards their accessible names.
			foreach (var commandName in new[] { "Open", "Cut", "Copy", "Rename", "Delete", "Properties", "Show more options" })
			{
				TestHelper.WaitForElementByName(commandName);
			}

			// Check for accessibility issues while the menu is open
			AxeHelper.AssertNoAccessibilityErrors();

			// Expanding Show more options must reveal its items (built-in overflow commands plus
			// whatever shell extensions loaded)
			AssertSubMenuLoadsItems("Show more options", () => TestHelper.ContextClickElementByName(folderName));

			// Close the menu
			TestHelper.SendEscKey();
			Thread.Sleep(200);

			// Keyboard invocation: Shift+F10 on the selected item must open the same menu
			TestHelper.InvokeButtonByName(folderName);
			new Actions(SessionManager.Session).KeyDown(Keys.Shift).SendKeys(Keys.F10).KeyUp(Keys.Shift).Perform();
			TestHelper.WaitForElementByName("Open");
			TestHelper.SendEscKey();
			Thread.Sleep(200);
		}

		/// <summary>
		/// Tests the image-specific context menu commands on an image file.
		/// </summary>
		[TestMethod]
		public void TestImageFileContextMenu()
		{
			var fileName = $"Image {DateTime.UtcNow:yyyyMMddHHmmssfff}.png";

			NavigateToTestFolder();
			CreateItemFromNewFlyout("File", fileName);

			// Right-click the image and verify the image-only commands appear
			TestHelper.ContextClickElementByName(fileName);
			foreach (var commandName in new[] { "Set as", "Rotate left", "Rotate right" })
			{
				TestHelper.WaitForElementByName(commandName);
			}

			// The Set as submenu must expand and offer its targets
			AssertSubMenuLoadsItems("Set as", () => TestHelper.ContextClickElementByName(fileName));

			AxeHelper.AssertNoAccessibilityErrors();
			TestHelper.SendEscKey();
			Thread.Sleep(200);
		}

		/// <summary>
		/// Tests that the Open with and Send to submenus of a file's context menu load their
		/// async shell-provided items once expanded.
		/// </summary>
		[TestMethod]
		public void TestOpenWithAndSendToSubMenus()
		{
			var fileName = $"File {DateTime.UtcNow:yyyyMMddHHmmssfff}.txt";

			// Open with only exists for files
			NavigateToTestFolder();
			CreateItemFromNewFlyout("File", fileName);

			// Right-click the file and wait for the menu
			TestHelper.ContextClickElementByName(fileName);
			TestHelper.WaitForElementByName("Open with");

			// Expanding Open with must reveal at least one loaded app entry
			AssertSubMenuLoadsItems("Open with", () => TestHelper.ContextClickElementByName(fileName), allowLeafFallback: true);

			// Expanding Send to must reveal at least one loaded target entry
			AssertSubMenuLoadsItems("Send to", () => TestHelper.ContextClickElementByName(fileName), allowLeafFallback: true);

			// Close the menu
			TestHelper.SendEscKey();
			Thread.Sleep(200);
		}

		/// <summary>
		/// Tests that the primary icon-button row opens adjacent to the pointer: at the top of the
		/// menu when it opens downward (click high on the page) and at the bottom when the menu
		/// flips above the pointer (click near the bottom edge of the window).
		/// </summary>
		[TestMethod]
		public void TestPrimaryCommandRowOpensNearPointer()
		{
			const int maxDistance = 150;
			var folderName = $"Row {DateTime.UtcNow:yyyyMMddHHmmssfff}";

			NavigateToTestFolder();
			CreateItemFromNewFlyout("InnerNavigationToolbarNewFolderButton", folderName);

			// Downward open: right-click the item near the top of the list. The buttons are looked up
			// by automation id - a name lookup can land on the toolbar's identically-named buttons.
			var item = TestHelper.GetElementByName(folderName);
			var itemClickY = item.Location.Y + item.Size.Height / 2;
			TestHelper.ContextClickElement(item);
			TestHelper.WaitForElementByName("Open");
			var cutButton = TestHelper.GetElementById("ContextMenuPrimaryButton_Cut");
			var cutCenterY = cutButton.Location.Y + cutButton.Size.Height / 2;
			Assert.IsTrue(Math.Abs(cutCenterY - itemClickY) <= maxDistance,
				$"Item menu: primary row is {Math.Abs(cutCenterY - itemClickY)}px from the click point (max {maxDistance}px).");
			CloseMenu();

			// Upward open: right-click empty space near the bottom of the window - the menu flips
			// above the pointer there, so the row must sit at its bottom, still near the pointer
			var windowRect = SessionManager.Session.Manage().Window;
			var bottomClickY = windowRect.Position.Y + windowRect.Size.Height - 120;
			var itemCenterY = item.Location.Y + item.Size.Height / 2;
			new Actions(SessionManager.Session)
				.MoveToElement(item)
				.MoveByOffset(0, bottomClickY - itemCenterY)
				.ContextClick()
				.Perform();
			TestHelper.WaitForElementByName("Layout");
			var propertiesButton = TestHelper.GetElementById("ContextMenuPrimaryButton_Properties");
			var propertiesCenterY = propertiesButton.Location.Y + propertiesButton.Size.Height / 2;
			Assert.IsTrue(Math.Abs(propertiesCenterY - bottomClickY) <= maxDistance,
				$"Background menu: primary row is {Math.Abs(propertiesCenterY - bottomClickY)}px from the click point (max {maxDistance}px).");
			CloseMenu();
		}

		/// <summary>
		/// Tests that a primary command's keyboard accelerator invokes the command AND closes the menu
		/// while the menu is open. The primary commands render as icon buttons, so their accelerators
		/// live on those buttons; without them a shortcut like F2 would run through the global handler
		/// and leave the menu open. F2 (Rename) is used because it enters inline-rename mode, a visible
		/// side effect that also proves the menu closed (rename cannot start with the menu still up).
		/// </summary>
		[TestMethod]
		public void TestPrimaryCommandAcceleratorClosesMenu()
		{
			var folderName = $"Accel {DateTime.UtcNow:yyyyMMddHHmmssfff}";

			NavigateToTestFolder();
			CreateItemFromNewFlyout("InnerNavigationToolbarNewFolderButton", folderName);

			// Open the item's context menu
			TestHelper.ContextClickElementByName(folderName);
			TestHelper.WaitForElementByName("Open");

			// Press the Rename accelerator (F2) while the menu is open
			new Actions(SessionManager.Session).SendKeys(Keys.F2).Perform();

			// The command ran (inline rename started) and the menu is gone
			var renameBox = TestHelper.GetElementById("ItemNameTextBox");
			Assert.IsFalse(TestHelper.ElementExistsByName("Open", 500),
				"The context menu is still open after invoking a primary command's accelerator.");

			// Cancel rename so the folder keeps its name for the shared-folder cleanup
			renameBox.SendKeys(Keys.Escape);
			Thread.Sleep(150);
		}

		/// <summary>
		/// Navigates into the shared test folder (creating it on first use), unless a previous
		/// test already left the file area there.
		/// </summary>
		private static void NavigateToTestFolder()
		{
			if (isInTestFolder)
				return;

			Directory.CreateDirectory(testFolderPath);
			TestHelper.NavigateToPath(testFolderPath);
			Thread.Sleep(300);
			isInTestFolder = true;
		}

		/// <summary>
		/// Expands the given submenu of an open context menu, asserts that new menu items appear
		/// inside it, and collapses it again so the next expansion starts from a clean state.
		/// The contents load asynchronously, so the expansion is retried; when the menu itself got
		/// dismissed in the meantime (focus churn on CI machines), <paramref name="reopenMenu"/>
		/// brings it back.
		/// </summary>
		private static void AssertSubMenuLoadsItems(string subMenuName, Action reopenMenu = null, bool allowLeafFallback = false)
		{
			var baseline = TestHelper.GetElementsOfType("MenuItem").Count;
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
			var loaded = false;

			while (DateTime.UtcNow < deadline)
			{
				if (reopenMenu is not null && !TestHelper.ElementExistsByName(subMenuName, 1500))
				{
					reopenMenu();
					continue;
				}

				// Only Open with / Send to may fall back to a leaf when the shell offers no sub-items (common on
				// bare CI images); a leaf on any other submenu is a real regression, so it is not excused here.
				if (allowLeafFallback && TestHelper.IsLeafMenuItem(subMenuName))
					return;

				// Only click while collapsed; clicking an already-expanded submenu collapses it again
				if (!TestHelper.IsMenuItemExpanded(subMenuName))
					TestHelper.InvokeButtonByName(subMenuName);
				Thread.Sleep(400);

				if (TestHelper.GetElementsOfType("MenuItem").Count > baseline)
				{
					loaded = true;
					break;
				}
			}

			// Collapse the submenu (Esc closes only the innermost open flyout)
			TestHelper.SendEscKey();
			Thread.Sleep(150);

			if (!loaded)
				Assert.Fail($"The '{subMenuName}' submenu did not load any items. Visible menu items: {TestHelper.DescribeVisibleMenuItems()}");
		}

		/// <summary>
		/// Expands and verifies the given submenu when the open menu offers it; menus differ per
		/// item type, so absence is not a failure.
		/// </summary>
		private static void ExpandSubMenuIfPresent(string subMenuName, Action reopenMenu = null, bool allowLeafFallback = false)
		{
			if (TestHelper.ElementExistsByName(subMenuName, 700))
				AssertSubMenuLoadsItems(subMenuName, reopenMenu, allowLeafFallback);
		}

		/// <summary>
		/// Fully closes an open context menu (a first Esc may only close an open submenu).
		/// </summary>
		private static void CloseMenu()
		{
			TestHelper.SendEscKey();
			Thread.Sleep(120);
			TestHelper.SendEscKey();
			Thread.Sleep(180);
		}

		/// <summary>
		/// Creates an item via the toolbar's New flyout ("InnerNavigationToolbarNewFolderButton"
		/// for folders, "File" for files) and waits until it appears in the file area.
		/// </summary>
		private static void CreateItemFromNewFlyout(string flyoutItemId, string itemName)
		{
			TestHelper.InvokeButtonById("InnerNavigationToolbarNewButton");
			TestHelper.InvokeButtonById(flyoutItemId);
			TestHelper.SetTextById("CreateItemDialogNameTextBox", itemName);
			TestHelper.InvokeDialogPrimaryButton("Create");

			// Wait for the dialog to fully close before touching the new item - its name matches
			// the dialog's text box, and clicks through the fading dialog are not interactable
			TestHelper.WaitUntilElementGoneById("CreateItemDialogNameTextBox");
			TestHelper.WaitForElementByName(itemName);
		}
	}
}
