using Files.App.Services.Settings;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Files.App.Helpers;

namespace Files.App.Helpers
{
	public static class AppLifecycleHelper
	{
		private const long defaultBufferSize = 1024;

		private const string sharedMemoryHeaderName = "FilesAppTabsWithID";
		private static MemoryMappedFile? sharedMemoryHeader;

		private static long sharedMemoryNameDefaultSuffix = 0;
		private static string defaultSharedMemoryName = sharedMemoryHeaderName + sharedMemoryNameDefaultSuffix.ToString();
		private static long sharedMemoryNameSuffix = sharedMemoryNameDefaultSuffix;
		private static long sharedMemoryNameSuffixStep = 1;
		private static string sharedMemoryName = defaultSharedMemoryName;
		public static string instanceId = Process.GetCurrentProcess().Id.ToString();
		private static MemoryMappedFile? sharedMemory;

		private static IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static List<TabItemWithIDArguments> tabsWithIdArgList = new List<TabItemWithIDArguments>();
	
		// Add sharedMemoryHeaderName because sharedMemory can't be released instantly and create a new one with the same name.
		// To dynamically expand the size of a sharedMemory, a new sharedMemory with a new name needs to be created.
		// Using sharedMemoryHeader, sharedMemoryName is saved and shared between all the Files instance.
		// Read or create a sharedMemory(sharedMemoryHeaderName) to save and share the sharedMemoryName
		// The sharedMemoryName is used to identify the sharedMemory
		// The sharedMemory is used to save and share the TabItemWithIDArguments

		private static bool WriteMemory(MemoryMappedFile memory, string dataBuffer)
		{
			try
			{
				var accessor = memory.CreateViewAccessor();
				byte[] buffer = Encoding.UTF8.GetBytes(dataBuffer);
				if (buffer.Length > accessor.Capacity)
					return false;
				accessor.WriteArray(0, buffer, 0, buffer.Length);
				accessor.Write(buffer.Length, (byte)'\0');
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static string ReadMemory(MemoryMappedFile memory)
		{
			var accessor = memory.CreateViewAccessor();
			var buffer = new byte[accessor.Capacity];
			accessor.ReadArray(0, buffer, 0, buffer.Length);
			var nullIndex = Array.IndexOf(buffer, (byte)'\0');
			var truncatedBuffer = new byte[nullIndex];
			Array.Copy(buffer, 0, truncatedBuffer, 0, truncatedBuffer.Length);
			return Encoding.UTF8.GetString(truncatedBuffer);
		}

		private static bool CheckMemory(string memoryName)
		{
			try
			{
				var memory = MemoryMappedFile.OpenExisting(memoryName);
				return true;
			}
			catch (FileNotFoundException)
			{
				return false;
			}
		}

		private static long GetSharedMemoryNameSuffix()
		{
			sharedMemoryNameSuffix = long.Parse(sharedMemoryName.Substring(sharedMemoryHeaderName.Length));
			return sharedMemoryNameSuffix;
		}

		/// <summary>
		/// Get sharedMemoryName from sharedMemoryHeader. If sharedMemoryHeader doesn't exist, create it.
		/// </summary>
		private static string GetSharedMemoryName()
		{
			try
			{
				sharedMemoryHeader = MemoryMappedFile.OpenExisting(sharedMemoryHeaderName);
				sharedMemoryName = ReadMemory(sharedMemoryHeader);
				sharedMemoryNameSuffix = GetSharedMemoryNameSuffix();
			}
			catch (FileNotFoundException)
			{
				sharedMemoryName = defaultSharedMemoryName;
				sharedMemoryNameSuffix = sharedMemoryNameDefaultSuffix;
				WriteSharedMemoryName(sharedMemoryName);
			}
			return sharedMemoryName;
		}

		/// <summary>
		/// Write sharedMemoryName to sharedMemoryHeader.
		/// </summary>
		private static void WriteSharedMemoryName(string sharedMemoryNameIn)
		{
			sharedMemoryHeader = MemoryMappedFile.CreateOrOpen(sharedMemoryHeaderName, defaultBufferSize);
			WriteMemory(sharedMemoryHeader, sharedMemoryNameIn);
		}

		/// <summary>
		/// Get current used sharedMemory reference. If current sharedMemory is disposed, try the previous one. 
		/// </summary>
		private static MemoryMappedFile GetSharedMemory()
		{
			sharedMemoryName = GetSharedMemoryName();
			var isDone = false;
			for (var i = sharedMemoryNameSuffix; i >= sharedMemoryNameDefaultSuffix; i--)
			{
				sharedMemoryName = sharedMemoryHeaderName + i.ToString();
				if (CheckMemory(sharedMemoryName))
				{
					sharedMemoryName = sharedMemoryHeaderName + i.ToString();
					sharedMemoryNameSuffix = i;
					isDone = true;
					break;
				}
			}
			if (!isDone)
			{
				sharedMemoryName = defaultSharedMemoryName;
				sharedMemoryNameSuffix = sharedMemoryNameDefaultSuffix;
			}
			WriteSharedMemoryName(sharedMemoryName);
			sharedMemory = MemoryMappedFile.CreateOrOpen(sharedMemoryName, defaultBufferSize);
			return sharedMemory;
		}

		/// <summary>
		/// Increase the size of sharedMemory.
		/// For dynamic expansion, a new sharedMemory with a unique name must be created. Transfer data from the old sharedMemory to the new one, and attempt to clear the old sharedMemory.
		/// Note that the old sharedMemory may still be in use by other instances. Therefore, if this instance creates a new sharedMemory, its record will be removed from the old sharedMemory.
		/// </summary>
		private static MemoryMappedFile ExtendSharedMemory(long newBufferSize)
		{
			sharedMemoryName = GetSharedMemoryName();
			sharedMemory = MemoryMappedFile.OpenExisting(sharedMemoryName);
			var memoryData = ReadMemory(sharedMemory);
			var tabsWithId = JsonSerializer.Deserialize<List<string>>(memoryData).Select(x => TabItemWithIDArguments.Deserialize(x)).ToList();
			var otherTabsWithId = RemoveTabsWithID(tabsWithId);
			var otherTabsWithIdStr = otherTabsWithId.Select(x => x.Serialize()).ToList();
			WriteMemory(sharedMemory, JsonSerializer.Serialize(otherTabsWithIdStr));
			sharedMemory.Dispose();
			sharedMemoryNameSuffix = sharedMemoryNameSuffix + sharedMemoryNameSuffixStep;
			sharedMemoryName = sharedMemoryHeaderName + sharedMemoryNameSuffix.ToString();
			sharedMemory = MemoryMappedFile.CreateOrOpen(sharedMemoryName, newBufferSize);
			WriteMemory(sharedMemory, memoryData);
			WriteSharedMemoryName(sharedMemoryName);
			return sharedMemory;
		}

		private static bool CheckMemorySize(MemoryMappedFile memory, long size)
		{
			var accessor = memory.CreateViewAccessor();
			if (accessor.Capacity < size)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Read tabsWithIdArgList from sharedMemory
		/// </summary>
		private static async Task ReadSharedMemory()
		{
			try
			{
				sharedMemory = GetSharedMemory();
				var bufferStr = ReadMemory(sharedMemory);
				if (string.IsNullOrEmpty(bufferStr))
				{
					tabsWithIdArgList = new List<TabItemWithIDArguments>();
					return;
				}
				tabsWithIdArgList = JsonSerializer.Deserialize<List<string>>(bufferStr).Select(x => TabItemWithIDArguments.Deserialize(x)).ToList();
			}
			finally
			{
				await Task.CompletedTask;
			}
		}

		/// <summary>
		/// Write tabsWithIdArgList to sharedMemory
		/// </summary>
		private static async Task WriteSharedMemory()
		{
			try
			{
				var tabsWithIDArgStrList = tabsWithIdArgList.Select(x => x.Serialize()).ToList();
				string bufferStr = JsonSerializer.Serialize(tabsWithIDArgStrList);
				sharedMemory = GetSharedMemory();
				if (!CheckMemorySize(sharedMemory, bufferStr.Length))
				{
					sharedMemory = ExtendSharedMemory(bufferStr.Length);
				}
				WriteMemory(sharedMemory, bufferStr);
			}
			finally
			{
				await Task.CompletedTask;
			}
		}

		private static List<TabItemWithIDArguments> AddTabsWithID()
		{
			var otherTabsWithIdArgList = tabsWithIdArgList.FindAll(x => x.instanceId != instanceId).ToList();
			var thisInstanceTabsStr = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(x => x.NavigationParameter.Serialize()).ToList();
			var thisInstanceTabsWithIdArgList = thisInstanceTabsStr.Select(x => TabItemWithIDArguments.CreateFromTabItemArg(CustomTabViewItemParameter.Deserialize(x))).ToList();
			var newTabsWithIDArgList = otherTabsWithIdArgList.ToList();
			newTabsWithIDArgList.AddRange(thisInstanceTabsWithIdArgList);
			return newTabsWithIDArgList;
		}

		private static List<TabItemWithIDArguments> RemoveTabsWithID(List<TabItemWithIDArguments> tabItemsList)
		{
			var otherTabsWithIDArgList = tabItemsList.FindAll(x => x.instanceId != instanceId).ToList();
			return otherTabsWithIDArgList;
		}

		/// <summary>
		/// Update the tabsWithIdArgList stored in sharedMemory and userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList.
		/// Should be executed once when a tab is changed.
		/// </summary>
		public static async Task UpDate()
		{
			await ReadSharedMemory();
			tabsWithIdArgList = AddTabsWithID();
			await WriteSharedMemory();
			await ReadSharedMemory();
			userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList = tabsWithIdArgList.Select(x => x.Serialize()).ToList();
		}

		/// <summary>
		/// Remove the tabs of the current instance from tabsWithIdArgList.
		/// Should be executed once when closing the current instance.
		/// </summary>
		public static async void RemoveThisInstanceTabs()
		{
			await ReadSharedMemory();
			tabsWithIdArgList = RemoveTabsWithID(tabsWithIdArgList).ToList();
			await WriteSharedMemory();
			userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList = tabsWithIdArgList.Select(x => x.Serialize()).ToList();
		}

		/// <summary>
		/// Compare tabsWithIdArgList and userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList in sharedMemory, and restore tabs that were not closed normally (direct shutdown, etc.).
		/// Should be executed once when starting a new instance.
		/// </summary>
		public static bool RestoreLastAppsTabs(MainPageViewModel mainPageViewModel)
		{
			ReadSharedMemory();
			if (userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList is null)
			{
				return false;
			}
			// Compare LastAppsTabsWithIDList with tabsWithIdArgList (running instances) to identify Tabs records that are not currently running, and restore them.
			var lastAppsTabsWithIdArgList = userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList
				.Select(x => TabItemWithIDArguments.Deserialize(x))
				.ToList();
			var tabsIdList = tabsWithIdArgList
				.Select(x => x.instanceId)
				.Distinct()
				.ToList();
			var tabsWithIdToBeRestored = lastAppsTabsWithIdArgList
				.Where(x => !tabsIdList.Contains(x.instanceId))
				.ToList();
			if (tabsWithIdToBeRestored.Count == 0)
			{
				return false;
			}
			var instanceIdList = tabsWithIdToBeRestored
				.Select(x => x.instanceId)
				.Distinct()
				.ToList();
			// Classify Tabs by instanceId and open Tabs with the same instanceId in the same window
			for(int i = 0; i < instanceIdList.Count; i++)
			{
				string instanceId = instanceIdList[i];
				var tabsWithThisIdToBeRestored = tabsWithIdToBeRestored
					.Where(x => x.instanceId == instanceId)
					.ToList();
				var tabsToBeRestored = tabsWithThisIdToBeRestored
					.Select(x => x.ExportToTabItemArg())
					.ToList();
				var tabsToBeRestoredStr = tabsToBeRestored
					.Select(x => x.Serialize())
					.ToList();
				// Place the Tabs for the first instanceId in this window; create new windows for the others
				if (i == 0)
				{
					foreach (var tabArgs in tabsToBeRestored)
					{
						mainPageViewModel.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
					}
				}
				else
				{
					NavigationHelpers.OpenTabsInNewWindowAsync(tabsToBeRestoredStr);
				}
			}
			return true;
		}
	}
}
