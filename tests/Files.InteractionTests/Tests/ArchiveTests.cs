// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
				WaitForCondition(() => File.Exists(extractedFilePath), $"'{name}' to be extracted");
				Assert.AreEqual(content, File.ReadAllText(extractedFilePath), $"The extracted file '{name}' does not match the original content.");
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

			TestHelper.ContextClickElementByName(itemName);

			while (true)
			{
				if (DateTime.UtcNow > deadline)
					Assert.Fail($"Could not invoke '{commandName}' from the '{subMenuName}' submenu of '{itemName}'.");

				// Reopen the menu when something (e.g. an async refresh) dismissed it
				if (!TestHelper.ElementExistsByName(subMenuName, 1500))
				{
					TestHelper.ContextClickElementByName(itemName);
					continue;
				}

				// Only click while collapsed; clicking an already-expanded submenu collapses it
				if (!TestHelper.IsMenuItemExpanded(subMenuName))
					TestHelper.InvokeButtonByName(subMenuName);

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
		/// Waits until the given disk-state condition holds; archive operations run in the
		/// background, so their completion is only observable through the file system.
		/// </summary>
		private static void WaitForCondition(Func<bool> condition, string description)
		{
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

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
