// Copyright (c) Files Community
// Licensed under the MIT License.

using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Files.InteractionTests.Helper
{
	public static class TestHelper
	{
		private static readonly TimeSpan DefaultFindTimeout = TimeSpan.FromSeconds(20);
		private static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromMilliseconds(500);

		/// <summary>
		/// Root folder that holds everything the tests create on disk, kept outside the user
		/// profile so test runs never touch personal folders such as Desktop or Documents.
		/// SessionManager creates it when the test run starts and deletes it when it ends.
		/// </summary>
		public static readonly string TestDataRootPath = @"C:\Temp\Files.InteractionTests";

		public static ICollection<WindowsElement> GetElementsOfType(string elementType)
		{
			try
			{
				return SessionManager.Session.FindElementsByTagName(elementType);
			}
			catch (OpenQA.Selenium.WebDriverException)
			{
				// The session can be left pointing at a closed popup window (context menus are
				// top-level windows); re-anchor to the app window and retry
				TryRecoverWindow();
				return SessionManager.Session.FindElementsByTagName(elementType);
			}
		}

		/// <summary>
		/// Re-anchors the session to the application's first (main) window after the window the
		/// session was tracking - typically a menu popup - has closed. Only acts when the tracked
		/// window is actually gone: switching windows activates the target, which would light-
		/// dismiss an open context menu.
		/// </summary>
		private static void TryRecoverWindow()
		{
			try
			{
				var handles = SessionManager.Session.WindowHandles;
				if (handles.Count == 0)
					return;

				string currentHandle = null;
				try
				{
					currentHandle = SessionManager.Session.CurrentWindowHandle;
				}
				catch (OpenQA.Selenium.WebDriverException)
				{
					// The tracked window is gone; fall through and re-anchor
				}

				// Only act when the tracked window is actually gone: switching windows activates
				// the target, which would light-dismiss an open context menu. WinAppDriver keeps
				// answering with the stale handle of a closed window, so absence from the list of
				// open windows is the reliable gone-signal.
				if (currentHandle is null || !handles.Contains(currentHandle))
					SessionManager.Session.SwitchTo().Window(handles[0]);
			}
			catch (Exception)
			{
				// Recovery is best-effort; the caller's retry loop reports the real failure
			}
		}

		public static List<WindowsElement> GetElementsOfTypeWithContent(string elementType, string content)
			=> GetItemsWithContent(GetElementsOfType(elementType), content);

		public static List<WindowsElement> GetItemsWithContent(ICollection<WindowsElement> elements, string content)
		{
			List<WindowsElement> elementsToReturn = [];
			foreach (WindowsElement element in elements)
			{
				if (element.Text.Contains(content, StringComparison.OrdinalIgnoreCase))
				{
					elementsToReturn.Add(element);
					continue;
				}
				// Check children if we did not find it in the items name
				System.Collections.ObjectModel.ReadOnlyCollection<OpenQA.Selenium.Appium.AppiumWebElement> children = element.FindElementsByTagName("Text");
				foreach (OpenQA.Selenium.Appium.AppiumWebElement child in children)
				{
					if (child.Text.Contains(content, StringComparison.OrdinalIgnoreCase))
					{
						elementsToReturn.Add(element);
					}
				}
			}
			return elementsToReturn;
		}

		public static void InvokeButtonByName(string uiaName)
			=> InteractWithRetry(() => FindElementByNameWithRetry(uiaName).Click(), $"click element name '{uiaName}'");

		public static void InvokeButtonById(string uiaName)
			=> InteractWithRetry(() => FindElementByIdWithRetry(uiaName).Click(), $"click element id '{uiaName}'");

		/// <summary>
		/// Retries a UI interaction until it succeeds. An element can be found but momentarily not
		/// interactable (e.g. a toolbar button still disabled while a navigation completes), which
		/// throws rather than waits.
		/// </summary>
		private static void InteractWithRetry(Action interaction, string description)
		{
			Exception lastException = null;
			var deadline = DateTime.UtcNow + DefaultFindTimeout;

			while (DateTime.UtcNow < deadline)
			{
				try
				{
					interaction();
					return;
				}
				catch (OpenQA.Selenium.WebDriverException ex)
				{
					lastException = ex;
					TryRecoverWindow();
					Thread.Sleep(DefaultRetryInterval);
				}
			}

			throw new TimeoutException($"Timed out trying to {description}.", lastException);
		}

		public static void WaitForElementByName(string uiaName)
			=> FindElementByNameWithRetry(uiaName);

		public static WindowsElement GetElementById(string automationId)
			=> FindElementByIdWithRetry(automationId);

		public static WindowsElement GetElementByName(string uiaName)
			=> FindElementByNameWithRetry(uiaName);

		/// <summary>
		/// Opens the item with the given UIA name by selecting it and pressing Enter.
		/// </summary>
		public static void OpenElementByName(string uiaName)
		{
			var element = FindElementByNameWithRetry(uiaName);
			element.Click();
			element.SendKeys(OpenQA.Selenium.Keys.Enter);
		}

		/// <summary>
		/// Navigates the file area to the given path the way a user would: Ctrl+L opens the
		/// Omnibar's path edit box, the path is typed there and submitted with Enter (the Omnibar
		/// navigates only on submit), then the wait ends once the toolbar reports the new folder.
		/// </summary>
		public static void NavigateToPath(string path)
		{
			// Already there - skip the edit-box round trip
			if (PathEquals(GetCurrentPath(), path))
				return;

			var deadline = DateTime.UtcNow + DefaultFindTimeout;

			// Enter path edit mode; re-send the shortcut until the edit box materializes, since
			// the keystroke is lost when the window does not have input focus yet
			while (true)
			{
				new OpenQA.Selenium.Interactions.Actions(SessionManager.Session)
					.KeyDown(OpenQA.Selenium.Keys.Control)
					.SendKeys("l")
					.KeyUp(OpenQA.Selenium.Keys.Control)
					.Perform();

				try
				{
					SessionManager.Session.FindElementByAccessibilityId("PART_TextBox");
					break;
				}
				catch (OpenQA.Selenium.WebDriverException)
				{
					if (DateTime.UtcNow > deadline)
						throw new TimeoutException($"Timed out opening the path edit box to navigate to '{path}'.");

					TryRecoverWindow();
					Thread.Sleep(DefaultRetryInterval);
				}
			}

			var pathBox = SetTextById("PART_TextBox", path);
			pathBox.SendKeys(OpenQA.Selenium.Keys.Enter);

			while (DateTime.UtcNow < deadline)
			{
				if (PathEquals(GetCurrentPath(), path))
					return;

				Thread.Sleep(DefaultRetryInterval);
			}

			throw new TimeoutException($"Timed out waiting for the app to navigate to '{path}'.");
		}

		private static bool PathEquals(string first, string second)
			=> first is not null && second is not null
				&& string.Equals(first.TrimEnd('\\'), second.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Returns the path the toolbar currently displays, or null while it is unavailable.
		/// </summary>
		private static string GetCurrentPath()
		{
			try
			{
				return SessionManager.Session.FindElementByAccessibilityId("CurrentPathGet").Text;
			}
			catch (OpenQA.Selenium.WebDriverException)
			{
				// The box goes stale while the toolbar rebuilds during navigation
				return null;
			}
		}

		/// <summary>
		/// Returns the first card-shaped button on the Home page in tree order, which is the first
		/// item of the topmost (pinned) widget.
		/// </summary>
		public static WindowsElement GetFirstWidgetCard()
			=> GetWidgetCard(static text => !string.IsNullOrEmpty(text));

		public static void ContextClickElementByName(string uiaName)
			=> InteractWithRetry(() => ContextClickElement(FindElementByNameWithRetry(uiaName)), $"right-click element name '{uiaName}'");

		public static void ContextClickElementById(string automationId)
			=> InteractWithRetry(() => ContextClickElement(FindElementByIdWithRetry(automationId)), $"right-click element id '{automationId}'");

		public static void ContextClickElement(WindowsElement element)
			=> new OpenQA.Selenium.Interactions.Actions(SessionManager.Session).ContextClick(element).Perform();

		/// <summary>
		/// Returns whether an element with the given UIA name exists within the given timeout.
		/// Unlike the other finders, absence is a valid result rather than a failure.
		/// </summary>
		public static bool ElementExistsByName(string uiaName, int timeoutMs)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

			while (DateTime.UtcNow < deadline)
			{
				try
				{
					SessionManager.Session.FindElementByName(uiaName);
					return true;
				}
				catch (Exception)
				{
					TryRecoverWindow();
					Thread.Sleep(DefaultRetryInterval);
				}
			}

			return false;
		}

		/// <summary>
		/// Returns the first card-shaped Home page button whose text contains the given content.
		/// </summary>
		public static WindowsElement GetWidgetCardByName(string content)
			=> GetWidgetCard(text => text.Contains(content, StringComparison.OrdinalIgnoreCase));

		/// <summary>
		/// Returns the first card-shaped Home page button whose text matches. The size bounds exclude
		/// toolbar buttons (smaller) and the full-width widget expander headers, whose center is empty space.
		/// </summary>
		private static WindowsElement GetWidgetCard(Func<string, bool> textMatches)
		{
			foreach (WindowsElement element in GetElementsOfType("Button"))
			{
				try
				{
					var size = element.Size;
					if (size.Width is >= 100 and <= 400 && size.Height is >= 60 and <= 220 && textMatches(element.Text))
						return element;
				}
				catch (Exception)
				{
					// Stale or unreadable element; keep scanning
				}
			}

			return null;
		}

		public static void SendEscKey()
			=> new OpenQA.Selenium.Interactions.Actions(SessionManager.Session).SendKeys(OpenQA.Selenium.Keys.Escape).Perform();

		/// <summary>
		/// Returns whether the menu item with the given name is a leaf item, i.e. has no submenu
		/// (no UIA ExpandCollapse pattern).
		/// </summary>
		public static bool IsLeafMenuItem(string uiaName)
		{
			try
			{
				var value = SessionManager.Session.FindElementByName(uiaName).GetAttribute("IsExpandCollapsePatternAvailable");
				return string.Equals(value, "False", StringComparison.OrdinalIgnoreCase);
			}
			catch (Exception)
			{
				// WebDriverException when the element vanished or the attribute is unsupported;
				// treat as expandable so the caller proceeds with its normal expansion path
				return false;
			}
		}

		/// <summary>
		/// Returns whether the submenu item with the given name is currently expanded.
		/// </summary>
		public static bool IsMenuItemExpanded(string uiaName)
		{
			try
			{
				var value = SessionManager.Session.FindElementByName(uiaName).GetAttribute("ExpandCollapse.ExpandCollapseState");
				return string.Equals(value, "Expanded", StringComparison.OrdinalIgnoreCase) || value == "1";
			}
			catch (Exception)
			{
				// WebDriverException when the element vanished or the attribute is unsupported;
				// treat as collapsed so the caller clicks to expand, its pre-existing behavior
				return false;
			}
		}

		/// <summary>
		/// Returns the names of all currently visible menu items, for assertion messages.
		/// </summary>
		public static string DescribeVisibleMenuItems()
		{
			try
			{
				var names = new List<string>();
				foreach (WindowsElement element in GetElementsOfType("MenuItem"))
				{
					try
					{
						names.Add($"'{element.Text}'");
					}
					catch (Exception)
					{
						// Stale element between enumeration and read
						names.Add("<unreadable>");
					}
				}

				return names.Count > 0 ? string.Join(", ", names) : "<none>";
			}
			catch (Exception)
			{
				// Diagnostics only; never mask the original assertion failure
				return "<unavailable>";
			}
		}

		/// <summary>
		/// Waits until the element with the given automation ID is gone, e.g. a closing dialog.
		/// Interacting with content underneath a dialog that is still fading out fails with
		/// "element is not pointer- or keyboard interactable".
		/// </summary>
		public static void WaitUntilElementGoneById(string automationId)
			=> WaitUntilElementGone(() => SessionManager.Session.FindElementByAccessibilityId(automationId));

		/// <summary>
		/// Waits until the element with the given UIA name is gone, e.g. an item being deleted.
		/// </summary>
		public static void WaitUntilElementGoneByName(string uiaName)
			=> WaitUntilElementGone(() => SessionManager.Session.FindElementByName(uiaName));

		private static void WaitUntilElementGone(Func<WindowsElement> find)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

			while (DateTime.UtcNow < deadline)
			{
				try
				{
					find();
					Thread.Sleep(200);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		/// <summary>
		/// Types text into the text box with the given automation ID and verifies the text
		/// actually landed there, retrying until it does. Raw keyboard input can be lost when
		/// focus is not where the sender assumes, so the write is confirmed by reading back.
		/// </summary>
		public static WindowsElement SetTextById(string automationId, string text)
		{
			Exception lastException = null;
			var deadline = DateTime.UtcNow + DefaultFindTimeout;

			while (DateTime.UtcNow < deadline)
			{
				try
				{
					var element = FindElementByIdWithRetry(automationId);
					element.Clear();
					element.SendKeys(text);

					if (element.Text == text)
						return element;
				}
				catch (Exception ex)
				{
					lastException = ex;
				}

				Thread.Sleep(DefaultRetryInterval);
			}

			throw new TimeoutException($"Timed out writing text into element id '{automationId}'.", lastException);
		}

		/// <summary>
		/// Clicks the primary button of the open ContentDialog. Pressing Enter instead is
		/// unreliable because the default button ignores it while disabled or unfocused.
		/// </summary>
		public static void InvokeDialogPrimaryButton(string fallbackButtonName)
		{
			try
			{
				InvokeButtonById("PrimaryButton");
			}
			catch (TimeoutException)
			{
				InvokeButtonByName(fallbackButtonName);
			}
		}

		private static WindowsElement FindElementByNameWithRetry(string name)
			=> FindWithRetry(() => SessionManager.Session.FindElementByName(name), $"element name '{name}'");

		private static WindowsElement FindElementByIdWithRetry(string id)
			=> FindWithRetry(() => SessionManager.Session.FindElementByAccessibilityId(id), $"element id '{id}'");

		private static WindowsElement FindWithRetry(Func<WindowsElement> find, string description)
		{
			Exception lastException = null;
			var deadline = DateTime.UtcNow + DefaultFindTimeout;

			while (DateTime.UtcNow < deadline)
			{
				try
				{
					return find();
				}
				catch (Exception ex)
				{
					lastException = ex;
					TryRecoverWindow();
					Thread.Sleep(DefaultRetryInterval);
				}
			}

			throw new TimeoutException($"Timed out waiting for {description}.", lastException);
		}
	}
}