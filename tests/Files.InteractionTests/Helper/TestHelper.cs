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

		public static ICollection<WindowsElement> GetElementsOfType(string elementType)
			=> SessionManager.Session.FindElementsByTagName(elementType);

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
			=> FindElementByNameWithRetry(uiaName).Click();

		public static void InvokeButtonById(string uiaName)
			=> FindElementByIdWithRetry(uiaName).Click();

		public static void WaitForElementByName(string uiaName)
			=> FindElementByNameWithRetry(uiaName);

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
		{
			Exception lastException = null;
			var deadline = DateTime.UtcNow + DefaultFindTimeout;

			while (DateTime.UtcNow < deadline)
			{
				try
				{
					return SessionManager.Session.FindElementByName(name);
				}
				catch (Exception ex)
				{
					lastException = ex;
					Thread.Sleep(DefaultRetryInterval);
				}
			}

			throw new TimeoutException($"Timed out waiting for element name '{name}'.", lastException);
		}

		private static WindowsElement FindElementByIdWithRetry(string id)
		{
			Exception lastException = null;
			var deadline = DateTime.UtcNow + DefaultFindTimeout;

			while (DateTime.UtcNow < deadline)
			{
				try
				{
					return SessionManager.Session.FindElementByAccessibilityId(id);
				}
				catch (Exception ex)
				{
					lastException = ex;
					Thread.Sleep(DefaultRetryInterval);
				}
			}

			throw new TimeoutException($"Timed out waiting for element id '{id}'.", lastException);
		}
	}
}