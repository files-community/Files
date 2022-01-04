using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace Files.InteractionTests
{
    [TestClass]
    internal class SessionManager
    {
        private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
        private const string FilesAppId = "FilesDev_ykqwq8d6ps0ag!App";


        private static WindowsDriver<WindowsElement> _session;
        public static WindowsDriver<WindowsElement> Session
        {
            get
            {
                if (_session == null)
                {
                    CreateSession(null);
                }
                return _session;
            }
        }

        [AssemblyInitialize]
        public static void CreateSession(TestContext _)
        {
            if (_session == null)
            {
                AppiumOptions appiumOptions = new AppiumOptions();
                appiumOptions.AddAdditionalCapability("app", FilesAppId);
                appiumOptions.AddAdditionalCapability("deviceName", "WindowsPC");
                try
                {
                    _session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appiumOptions);
                }
                catch (OpenQA.Selenium.WebDriverException) { }
                Thread.Sleep(3000);
                if (_session == null)
                {
                    // WinAppDriver is probably not running, so lets start it!
                    if(File.Exists(@"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"))
                    {
                        Process.Start(@"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe");
                    }
                    else if(File.Exists(@"C:\Program Files\Windows Application Driver\WinAppDriver.exe"))
                    {
                        Process.Start(@"C:\Program Files\Windows Application Driver\WinAppDriver.exe");
                    }
                    else
                    {
                        throw new Exception("Unable to start WinAppDriver since no suitable location was found.");
                    }

                    Thread.Sleep(10000);
                    _session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appiumOptions);
                }
                Assert.IsNotNull(_session);
                Assert.IsNotNull(_session.SessionId);

                // Dismiss the disclaimer window that may pop up on the very first application launch
                // If the disclaimer is not found, this throws an exception, so lets catch that
                try
                {
                    _session.FindElementByName("Disclaimer").FindElementByName("Accept").Click();
                }
                catch (OpenQA.Selenium.WebDriverException) { }

                // Wait if something is still animating in the visual tree
                _session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
                _session.Manage().Window.Maximize();

                AxeHelper.InitializeAxe();
            }
        }

        [AssemblyCleanup()]
        public static void TestRunTearDown()
        {
            TearDown();
        }

        public static void TearDown()
        {
            if (_session != null)
            {
                _session.CloseApp();
                _session.Quit();
                _session = null;
            }
        }
    }
}
