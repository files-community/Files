using Axe.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Files.InteractionTests
{
    internal class TestHelper
    {

        public static IScanner AccessibilityScanner;

        internal static void InitializeAxe()
        {
            var processes = Process.GetProcessesByName("Files");
            Assert.IsTrue(processes.Length > 0);

            var config = Config.Builder.ForProcessId(processes[0].Id).Build();

            AccessibilityScanner = ScannerFactory.CreateScanner(config);
        }

        public static void VerifyNoAccessibilityErrors()
        {
            Assert.AreEqual(0, TestHelper.AccessibilityScanner.Scan().ErrorCount);
        }

        public static ICollection<WindowsElement> GetElementsOfType(string elementType)
            => TestRunInitializer.Session.FindElementsByTagName(elementType);

        public static List<WindowsElement> GetElementsOfTypeWithContent(string elementType, string content)
            => GetItemsWithContent(GetElementsOfType(elementType), content);

        public static List<WindowsElement> GetItemsWithContent(ICollection<WindowsElement> elements, string content)
        {
            List<WindowsElement> elementsToReturn = new List<WindowsElement>();
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
                        continue;
                    }
                }
            }
            return elementsToReturn;
        }

        public static void InvokeButton(string uiaName)
            => TestRunInitializer.Session.FindElementByName(uiaName).Click();
    }
}
