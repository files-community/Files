// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using OpenQA.Selenium.Appium;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class ArchiveTests
	{
		private static readonly string testFolderName = $"IT ArchiveTests {DateTime.UtcNow:yyyyMMddHHmmssfff}";
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
				// The app can briefly keep a change-watcher handle on the folder right after
				// navigating away; the assembly cleanup removes the whole root once the app closed
			}
		}

		[TestCleanup]
		public void Cleanup()
		{
			// Close any menu a failed assertion may have left open
			TestHelper.SendEscKey();
		}

		/// <summary>
		/// Tests creating a zip archive from a folder through the context menu and extracting it
		/// back, verifying the round-tripped files match the originals.
		/// </summary>
		[TestMethod]
		public void TestCompressAndExtractZipArchive()
			=> CompressAndExtractRoundTrip("ZipSource", "zip");

		/// <summary>
		/// Tests the same round trip through the 7z format.
		/// </summary>
		[TestMethod]
		public void TestCompressAndExtractSevenZipArchive()
			=> CompressAndExtractRoundTrip("SevenZipSource", "7z");

		[TestMethod]
		public void TestExtractBZip2Archive()
		{
			var (archivePath, expectedHash) = CreateBZip2Fixture("ExtractBZip2-ação", 128 * 1024 * 1024);
			TestHelper.NavigateToPath(Path.GetDirectoryName(archivePath));
			InvokeItemContextMenuCommand(Path.GetFileName(archivePath), "Extract", "Extract here");
			AssertOperationProgress(Path.GetFileName(archivePath), "Extracted");
			AssertExtractedHash(Path.ChangeExtension(archivePath, null), expectedHash);
		}

		[TestMethod]
		[DataRow(false, 128 * 1024 * 1024)]
		[DataRow(true, 1024 * 1024)]
		[DataRow(false, 0)]
		[DataRow(true, 0)]
		public void TestCopyFromBZip2Archive(bool copyIntoArchive, int size)
		{
			var (archivePath, expectedHash) = CreateBZip2Fixture($"CopyBZip2-{copyIntoArchive}-{size}", size);
			var destinationPath = Path.Combine(Path.GetDirectoryName(archivePath), $"Destination-CopyBZip2-{copyIntoArchive}-{size}");
			if (copyIntoArchive)
			{
				destinationPath += ".zip";
				using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create);
			}
			else
				Directory.CreateDirectory(destinationPath);

			TestHelper.NavigateToPath(archivePath);
			var fileName = Path.GetFileNameWithoutExtension(archivePath);
			TestHelper.InvokeButtonByName(fileName);
			TestHelper.SendKeyCombination(VIRTUAL_KEY.VK_CONTROL, VIRTUAL_KEY.VK_C);
			TestHelper.NavigateToPath(destinationPath);
			TestHelper.SendKeyCombination(VIRTUAL_KEY.VK_CONTROL, VIRTUAL_KEY.VK_V);
			if (!copyIntoArchive && size > 0)
				AssertOperationProgress(Path.GetFileName(destinationPath), "Copied");
			if (copyIntoArchive)
				AssertCopiedArchiveHash(destinationPath, fileName, expectedHash);
			else
				AssertExtractedHash(Path.Combine(destinationPath, fileName), expectedHash);

			TestHelper.SendEscKey();
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(17)]
		public void TestExtractSmallBZip2Archive(int size)
		{
			var (archivePath, expectedHash) = CreateBZip2Fixture($"SmallBZip2-{size}-ação", size);
			TestHelper.NavigateToPath(Path.GetDirectoryName(archivePath));
			InvokeItemContextMenuCommand(Path.GetFileName(archivePath), "Extract", "Extract here");
			AssertExtractedHash(Path.ChangeExtension(archivePath, null), expectedHash);
		}

		[TestMethod]
		public void TestCopyOrdinaryFileIntoZipArchive()
		{
			var directory = Path.Combine(testFolderPath, "OrdinaryCopy");
			Directory.CreateDirectory(directory);
			var fileName = "ordinary.bin";
			var data = new byte[1024 * 1024];
			new Random(18806).NextBytes(data);
			File.WriteAllBytes(Path.Combine(directory, fileName), data);
			var destinationPath = Path.Combine(directory, "OrdinaryDestination.zip");
			using (ZipFile.Open(destinationPath, ZipArchiveMode.Create)) { }

			TestHelper.NavigateToPath(directory);
			TestHelper.InvokeButtonByName(fileName);
			TestHelper.SendKeyCombination(VIRTUAL_KEY.VK_CONTROL, VIRTUAL_KEY.VK_C);
			TestHelper.NavigateToPath(destinationPath);
			TestHelper.SendKeyCombination(VIRTUAL_KEY.VK_CONTROL, VIRTUAL_KEY.VK_V);
			AssertCopiedArchiveHash(destinationPath, fileName, SHA256.HashData(data));
		}

		private static (string ArchivePath, byte[] Hash) CreateBZip2Fixture(string name, int size = 1024 * 1024)
		{
			var directory = Path.Combine(testFolderPath, name);
			Directory.CreateDirectory(directory);
			var archivePath = Path.Combine(directory, $"{name}.bin.BZ2");
			var data = new byte[size];
			new Random(18806).NextBytes(data);
			using var input = new MemoryStream(data);
			using var output = File.Create(archivePath);
			var preparation = Task.Run(() => BZip2.Compress(input, output, false, 9));
			while (!preparation.Wait(TimeSpan.FromSeconds(5)))
				_ = SessionManager.Session.WindowHandles;
			preparation.GetAwaiter().GetResult();
			return (archivePath, SHA256.HashData(data));
		}

		private static void AssertOperationProgress(string uniqueName, string completedPrefix)
		{
			if (SessionManager.Session.FindElements(MobileBy.AccessibilityId("StatusCenterItemsListView")).Count == 0)
				TestHelper.InvokeButtonById("ShowStatusCenterButton");

			double previous = 0;
			bool sawIntermediate = false;
			WaitForCondition(() =>
			{
				try
				{
					var cards = SessionManager.Session.FindElements(MobileBy.XPath($"//*[@AutomationId='ContentRoot' and contains(@Name, '{uniqueName}')]"));
					foreach (var card in cards)
					{
						var header = card.GetAttribute("Name");
						Assert.IsFalse(header.StartsWith("Error", StringComparison.Ordinal) || header.StartsWith("Canceled", StringComparison.Ordinal), header);
						if (header.StartsWith(completedPrefix, StringComparison.Ordinal))
						{
							Assert.IsTrue(sawIntermediate, $"'{uniqueName}' completed without visible intermediate progress.");
							return true;
						}

						foreach (var bar in card.FindElements(MobileBy.AccessibilityId("MainProgressBar")))
						{
							if (!bar.Displayed)
								continue;
							var value = double.Parse(bar.GetAttribute("RangeValue.Value"), CultureInfo.InvariantCulture);
							Assert.IsTrue(double.IsFinite(value) && value >= previous && value < 100, $"'{uniqueName}' active progress went from {previous} to {value}.");
							sawIntermediate |= value > 0;
							previous = value;
							Console.WriteLine($"{uniqueName}: {value}%");
						}
					}
				}
				catch (OpenQA.Selenium.StaleElementReferenceException)
				{
					// Completion replaces the active card; find the matching result next time.
				}
				return false;
			}, $"successful completion with intermediate progress for '{uniqueName}'", 60);
		}

		private static void AssertExtractedHash(string path, byte[] expectedHash)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
			while (true)
			{
				try
				{
					var actualHash = SHA256.HashData(File.ReadAllBytes(path));
					if (System.Linq.Enumerable.SequenceEqual(expectedHash, actualHash))
						break;

					if (DateTime.UtcNow >= deadline)
						CollectionAssert.AreEqual(expectedHash, actualHash, "Extracted content differs from the source.");

					Thread.Sleep(100);
				}
				catch (IOException) when (DateTime.UtcNow < deadline)
				{
					Thread.Sleep(100);
				}
			}

			TestHelper.SendEscKey();
		}

		private static void AssertCopiedArchiveHash(string archivePath, string fileName, byte[] expectedHash)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
			while (DateTime.UtcNow < deadline)
			{
				try
				{
					using var archive = ZipFile.OpenRead(archivePath);
					if (archive.GetEntry(fileName) is { } entry)
					{
						using var stream = entry.Open();
						CollectionAssert.AreEqual(expectedHash, SHA256.HashData(stream));
						return;
					}
				}
				catch (Exception ex) when ((ex is IOException or InvalidDataException) && DateTime.UtcNow < deadline)
				{
				}

				Thread.Sleep(100);
			}

			Assert.Fail("The destination ZIP never contained the copied file.");
		}

		private static void CompressAndExtractRoundTrip(string sourceFolderName, string extension)
		{
			var sourceFolderPath = Path.Combine(testFolderPath, sourceFolderName);
			var archiveName = $"{sourceFolderName}.{extension}";
			var archivePath = Path.Combine(testFolderPath, archiveName);

			// Per-format file names: both formats extract into the shared test folder, so
			// identical names would collide with a replace prompt on the second run
			var files = new Dictionary<string, string>
			{
				[$"{extension}-first.txt"] = $"The first file that goes into the {extension} archive.",
				[$"{extension}-second.txt"] = $"The second file that goes into the {extension} archive.",
			};

			// Create the source folder on disk and show it in the file area
			Directory.CreateDirectory(sourceFolderPath);
			foreach (var (name, content) in files)
				File.WriteAllText(Path.Combine(sourceFolderPath, name), content);
			TestHelper.NavigateToPath(testFolderPath);

			// Compress: right-click the folder and pick Compress → Create <name>.<extension>
			InvokeItemContextMenuCommand(sourceFolderName, "Compress", $"Create {archiveName}");

			// The archive lands on disk and shows up in the file area once compression completes
			WaitForCondition(() => File.Exists(archivePath), $"the archive '{archiveName}' to be created");
			TestHelper.WaitForElementByName(archiveName);

			// Extract: right-click the archive and pick Extract → Extract here. The originals
			// stay inside the source subfolder, so the extracted files at the folder root are
			// unambiguously the archive's content.
			InvokeItemContextMenuCommand(archiveName, "Extract", "Extract here");

			// A single compressed folder is stored as entries relative to the folder itself, so
			// "Extract here" lands the files directly in the current folder
			foreach (var (name, content) in files)
			{
				var extractedFilePath = Path.Combine(testFolderPath, name);
				Assert.AreEqual(content, ReadWhenAvailable(extractedFilePath), $"The extracted file '{name}' does not match the original content.");
			}

			// The extracted files also show up in the file area
			TestHelper.WaitForElementByName($"{extension}-first.txt");
		}

		/// <summary>
		/// Invokes a command from a submenu of an item's context menu, reopening the menu when
		/// something (e.g. an async file-area refresh) dismissed it mid-interaction.
		/// </summary>
		private static void InvokeItemContextMenuCommand(string itemName, string subMenuName, string commandName)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

			void OpenContextMenu() => TestHelper.ContextClickElementByName(itemName);

			OpenContextMenu();

			while (true)
			{
				if (DateTime.UtcNow > deadline)
					Assert.Fail($"Could not invoke '{commandName}' from the '{subMenuName}' submenu of '{itemName}'.");

				// Reopen the menu when something (e.g. an async refresh) dismissed it
				var subMenus = SessionManager.Session.FindElements(MobileBy.XPath($"//MenuItem[@Name='{subMenuName}']"));
				if (subMenus.Count == 0)
				{
					OpenContextMenu();
					continue;
				}

				// Only click while collapsed; clicking an already-expanded submenu collapses it
				var expandState = subMenus[0].GetAttribute("ExpandCollapse.ExpandCollapseState");
				if (expandState != "Expanded" && expandState != "1")
					subMenus[0].Click();

				if (TestHelper.ElementExistsByName(commandName, 2000))
				{
					TestHelper.InvokeButtonByName(commandName);
					return;
				}

				// The menu likely got dismissed; close any leftover state and retry
				TestHelper.SendEscKey();
				Thread.Sleep(200);
			}
		}

		/// <summary>
		/// Reads a file the extractor may still be writing. The entry appears on disk before its
		/// write handle is closed, so both the missing file and the sharing violation are retried.
		/// </summary>
		private static string ReadWhenAvailable(string path)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

			while (true)
			{
				try
				{
					return File.ReadAllText(path);
				}
				catch (IOException) when (DateTime.UtcNow < deadline)
				{
					Thread.Sleep(300);
				}
			}
		}

		/// <summary>
		/// Waits for a background archive operation to reach the expected state.
		/// </summary>
		private static void WaitForCondition(Func<bool> condition, string description, int timeoutSeconds = 30)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);

			while (DateTime.UtcNow < deadline)
			{
				if (condition())
					return;

				Thread.Sleep(300);
			}

			Assert.Fail($"Timed out waiting for {description}.");
		}
	}
}
