using Axe.Windows.Automation;
using Axe.Windows.Core.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            var testResult = TestHelper.AccessibilityScanner.Scan().Errors.Where(error => error.Rule.ID != RuleId.BoundingRectangleNotNull);
            if(testResult.Count() != 0)
            {
                var mappedResult = testResult.Select(result => "Element " + result.Element.Properties["ControlType"] + " violated rule '" + result.Rule.Description + "'.");
                Assert.Fail("Failed with the following accessibility errors \r\n" + string.Join("\r\n", mappedResult));
            }
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
