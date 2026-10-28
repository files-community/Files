// Copyright (c) Files Community
// Licensed under the MIT License.

using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace Files.InteractionTests
{
	[TestClass]
	public sealed class SessionManager
	{
		private const string AppiumServerUrl = "http://127.0.0.1:4723";
		private static string[] FilesAppIDs = [
			"FilesDev_ykqwq8d6ps0ag!App", // Needed to run on the local end and/or the CI
			"FilesDev_9bhem8es8z4gp!App", // Needed to run on the local end and/or the CI
			"FilesDev_dwm5abbcs5pn0!App", // Needed to run on the CI
		];

		private static uint appIdIndex = 0;

		private static WindowsDriver _session;
		public static WindowsDriver Session
		{
			get
			{
				if (_session is null)
				{
					CreateSession(null);
				}
				return _session;
			}
		}

		private static void tryInitializeSession()
		{
			AppiumOptions appiumOptions = new AppiumOptions();
			appiumOptions.AutomationName = "Windows";
			appiumOptions.App = FilesAppIDs[appIdIndex];
			appiumOptions.DeviceName = "WindowsPC";
			try
			{
				_session = new WindowsDriver(new Uri(AppiumServerUrl), appiumOptions);
			}
			catch (OpenQA.Selenium.WebDriverException exc)
			{
				// Use next app ID since the current one was failing
				if (exc.Message.Contains("Package was not found"))
				{
					appIdIndex++;
				}
				else
				{
					Console.WriteLine("Failed to update start driver, got exception:" + exc.Message);
				}
			}
		}

		private static bool IsAppiumServerRunning()
		{
			try
			{
				using var client = new HttpClient();
				return client.GetAsync($"{AppiumServerUrl}/status").GetAwaiter().GetResult().IsSuccessStatusCode;
			}
			catch (HttpRequestException)
			{
				return false;
			}
		}

		[AssemblyInitialize]
		public static void CreateSession(TestContext _)
		{
			Directory.CreateDirectory(TestHelper.TestDataRootPath);

			if (_session is null)
			{

				int timeoutCount = 50;

				tryInitializeSession();
				if (_session is null && !IsAppiumServerRunning())
				{
					// Appium is not running, so lets start it!
					// Shell-executed + hidden so the server gets its own (hidden) console instead
					// of spamming its per-request log into the test output
					try
					{
						Process.Start(new ProcessStartInfo
						{
							FileName = "appium.cmd",
							UseShellExecute = true,
							WindowStyle = ProcessWindowStyle.Hidden,
						});
					}
					catch (Win32Exception)
					{
						throw new Exception("Unable to start Appium since it was not found on the PATH.");
					}

					var serverDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
					while (!IsAppiumServerRunning() && DateTime.UtcNow < serverDeadline)
						Thread.Sleep(250);

					tryInitializeSession();
				}

				while (_session is null && timeoutCount < 1000 * 4)
				{
					tryInitializeSession();
					Thread.Sleep(timeoutCount);
					timeoutCount *= 2;
				}

				Thread.Sleep(1000);
				Assert.IsNotNull(_session);
				Assert.IsNotNull(_session.SessionId);

				// Dismiss the disclaimer window that may pop up on the very first application launch
				// If the disclaimer is not found, this throws an exception, so lets catch that
				try
				{
					_session.FindElement(MobileBy.Name("Disclaimer")).FindElement(MobileBy.Name("Accept")).Click();
				}
				catch (OpenQA.Selenium.WebDriverException) { }

				// The window the session attached to (splash or disclaimer) may have closed since;
				// anchor to the first open window so the tests start against the main window
				try
				{
					_session.SwitchTo().Window(_session.WindowHandles[0]);
				}
				catch (OpenQA.Selenium.WebDriverException) { }

				// Kept short: the helpers retry their finds client-side, and a long implicit wait
				// stalls every negative lookup (absence checks, gone-waits) by its full duration
				_session.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);
				_session.Manage().Window.Maximize();

				AxeHelper.InitializeAxe();
			}
		}

		[AssemblyCleanup()]
		public static void TestRunTearDown()
		{
			try
			{
				TearDown();
			}
			finally
			{
				try
				{
					Directory.Delete(TestHelper.TestDataRootPath, true);
				}
				catch (DirectoryNotFoundException)
				{
					// The run created nothing on disk
				}
				catch (IOException)
				{
					// The closing app can briefly keep a change-watcher handle on a test folder;
					// the next run deletes the leftovers when its own cleanup runs
				}
			}
		}

		public static void TearDown()
		{
			if (_session is not null)
			{
				try
				{
					_session.CloseApp();
				}
				catch (OpenQA.Selenium.WebDriverException)
				{
					// The app already exited (or crashed); still quit to dispose the session
				}

				try
				{
					_session.Quit();
				}
				catch (OpenQA.Selenium.WebDriverException)
				{
					// The driver may already have dropped the session
				}

				_session = null;
			}
		}
	}
}
