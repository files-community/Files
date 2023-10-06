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
	public class AppLifecycle
	{
		public static string SharedMemoryNameHeader = "FilesAppTabsWithID";
		public static MemoryMappedFile? SharedMemoryNameHeaderMemory;
		public static string SharedMemoryName = SharedMemoryNameHeader + defaultBufferSize.ToString();
		public static string InstanceID = Process.GetCurrentProcess().Id.ToString();
		public static List<TabItemWithIDArguments> TabsWithIDArgList = new List<TabItemWithIDArguments>();
		public static MemoryMappedFile? SharedMemory;
		public static long defaultBufferSize = 1024;
		public static IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		/* 
		Add SharedMemoryNameHeader because SharedMemory can't be released instantly and create a new one with the same name.
		To dynamically expand the size of a SharedMemory, a new SharedMemory with a new name needs to be created.
		Using SharedMemoryNameHeaderMemory, SharedMemoryName is saved and shared between all the Files instance.
		*/
		// Read or create the a SharedMemory(SharedMemoryNameHeader) to save and share the SharedMemoryName
		// The SharedMemoryName is used to identify the SharedMemory
		// The SharedMemory is used to save and share the TabItemWithIDArguments
		protected static MemoryMappedFile GetSharedMemoryNameHeader()
		{
			try
			{
				SharedMemoryNameHeaderMemory = MemoryMappedFile.OpenExisting(SharedMemoryNameHeader);
				using (MemoryMappedViewAccessor accessor = SharedMemoryNameHeaderMemory.CreateViewAccessor())
				{
					long length = accessor.Capacity;
					byte[] buffer = new byte[length];
					accessor.ReadArray(0, buffer, 0, buffer.Length);
					int nullIndex = Array.IndexOf(buffer, (byte)'\0');
					if (nullIndex > 0)
					{
						int Index = nullIndex;
						byte[] truncatedBuffer = new byte[Index];
						Array.Copy(buffer, 0, truncatedBuffer, 0, Index);
						string bufferStr = Encoding.UTF8.GetString(truncatedBuffer);
						SharedMemoryName = bufferStr;
					}
					else
					{
						SharedMemoryName = SharedMemoryNameHeader + defaultBufferSize.ToString();
					}
				}
			}
			catch (FileNotFoundException)
			{
				SharedMemoryName = SharedMemoryNameHeader + defaultBufferSize.ToString();
				SharedMemoryNameHeaderMemory = MemoryMappedFile.CreateOrOpen(SharedMemoryNameHeader, 1024);
				using (MemoryMappedViewAccessor accessor = SharedMemoryNameHeaderMemory.CreateViewAccessor())
				{
					byte[] buffer = Encoding.UTF8.GetBytes(SharedMemoryName);
					accessor.WriteArray(0, buffer, 0, buffer.Length);
				}
			}
			return SharedMemoryNameHeaderMemory;
		}



		//	Check if the SharedMemory exists, if not create it
		protected static MemoryMappedFile CheckSharedMemory()
		{
			GetSharedMemoryNameHeader();
			try
			{
				SharedMemory = MemoryMappedFile.OpenExisting(SharedMemoryName);
			}
			catch (FileNotFoundException)
			{
				SharedMemory = MemoryMappedFile.CreateOrOpen(SharedMemoryName, defaultBufferSize);
			}
			return SharedMemory;
		}

		//	Check if the SharedMemory exists and if the BufferSize is enough, if not create a new one
		protected static MemoryMappedFile CheckSharedMemory(int BufferSize)
		{
			SharedMemory = CheckSharedMemory();
			long BufferSizeIn = BufferSize;
			using (MemoryMappedViewAccessor accessor0 = SharedMemory.CreateViewAccessor())
			{
				long length = accessor0.Capacity;
				if (length > BufferSizeIn)
				{
				}
				else
				{
					SharedMemory.Dispose();
					long NewBufferSize = ((BufferSizeIn / defaultBufferSize) + 1) * defaultBufferSize;
					SharedMemoryName = SharedMemoryNameHeader + NewBufferSize.ToString();
					SharedMemory = MemoryMappedFile.CreateOrOpen(SharedMemoryName, NewBufferSize);
					SharedMemoryNameHeaderMemory = MemoryMappedFile.CreateOrOpen(SharedMemoryNameHeader, 1024);
					using (MemoryMappedViewAccessor accessor1 = SharedMemoryNameHeaderMemory.CreateViewAccessor())
					{
						byte[] buffer = Encoding.UTF8.GetBytes(SharedMemoryName);
						accessor1.WriteArray(0, buffer, 0, buffer.Length);
					}
				}
			}
			return SharedMemory;
		}

		//	Read TabsWithIDArgList from SharedMemory
		protected static async Task ReadSharedMemory()
		{
			try
			{
				SharedMemory = CheckSharedMemory();
				using (MemoryMappedViewAccessor accessor = SharedMemory.CreateViewAccessor())
				{
					long length = accessor.Capacity;
					byte[] buffer = new byte[length];
					accessor.ReadArray(0, buffer, 0, buffer.Length);
					int nullIndex = Array.IndexOf(buffer, (byte)'\0');
					if (nullIndex > 0)
					{
						int Index = nullIndex;
						byte[] truncatedBuffer = new byte[Index];
						Array.Copy(buffer, 0, truncatedBuffer, 0, Index);
						string bufferStr = Encoding.UTF8.GetString(truncatedBuffer);
						List<string> TabsWithIDArgStrList = JsonSerializer.Deserialize<List<string>>(bufferStr);
						TabsWithIDArgList = TabsWithIDArgStrList.Select(x => TabItemWithIDArguments.Deserialize(x)).ToList();
					}
					else
					{
						TabsWithIDArgList = new List<TabItemWithIDArguments>();
					}
				}
			}
			finally
			{
				await Task.CompletedTask;
			}
		}

		//	Write TabsWithIDArgList to SharedMemory
		protected static async Task WriteSharedMemory()
		{
			try
			{
				List<string> TabsWithIDArgStrList = TabsWithIDArgList.Select(x => x.Serialize()).ToList();
				string bufferStr = JsonSerializer.Serialize(TabsWithIDArgStrList);
				byte[] buffer = Encoding.UTF8.GetBytes(bufferStr);

				SharedMemory = CheckSharedMemory(buffer.Length);

				using (MemoryMappedViewAccessor accessor = SharedMemory.CreateViewAccessor())
				{
					byte[] bufferClear = new byte[accessor.Capacity];
					accessor.WriteArray(0, bufferClear, 0, bufferClear.Length);
					accessor.WriteArray(0, buffer, 0, buffer.Length);
				}
			}
			finally
			{
				await Task.CompletedTask;
			}
		}

		//	Add Tabs of this instance to TabsWithIDList
		private static List<TabItemWithIDArguments> AddTabsWithID()
		{
			List<TabItemWithIDArguments> OtherTabsWithIDArgList = TabsWithIDArgList.FindAll(x => x.InstanceID != InstanceID).ToList();
			List<string> ThisInstanceTabsStr = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(x => x.NavigationParameter.Serialize()).ToList();

			List<TabItemWithIDArguments> ThisInstanceTabsWithIDArgList = ThisInstanceTabsStr.Select(x => TabItemWithIDArguments.Deserialize(x)).ToList();
			List<TabItemWithIDArguments> NewTabsWithIDArgList = OtherTabsWithIDArgList.ToList();
			NewTabsWithIDArgList.AddRange(ThisInstanceTabsWithIDArgList);
			return NewTabsWithIDArgList;
		}
		private static List<TabItemWithIDArguments> RemoveTabsWithID()
		{
			List<TabItemWithIDArguments> OtherTabsWithIDArgList = TabsWithIDArgList.FindAll(x => x.InstanceID != InstanceID).ToList();
			return OtherTabsWithIDArgList;
		}
		public static async Task UpDate()
		{
			await ReadSharedMemory();
			TabsWithIDArgList = AddTabsWithID();
			await WriteSharedMemory();
			await ReadSharedMemory();
			userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList = TabsWithIDArgList.Select(x => x.Serialize()).ToList();
		}
		public static async void RemoveThisInstanceTabs()
		{
			await ReadSharedMemory();
			TabsWithIDArgList = RemoveTabsWithID().ToList();
			await WriteSharedMemory();
			userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList = TabsWithIDArgList.Select(x => x.Serialize()).ToList();
		}

		//	Restore LastAppsTabs
		public static bool RestoreLastAppsTabs()
		{
			ReadSharedMemory();
			if (userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList is null)
			{
				return false;
			}
			List<TabItemWithIDArguments> LastAppsTabsWithIDArgList = userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList
				.Select(x => TabItemWithIDArguments.Deserialize(x))
				.ToList();
			List<string> TabsIDList = TabsWithIDArgList
				.Select(x => x.InstanceID)
				.Distinct()
				.ToList();
			List<TabItemWithIDArguments> TabsWithIDToBeRestored = LastAppsTabsWithIDArgList
				.Where(x => !TabsIDList.Contains(x.InstanceID))
				.ToList();
			if (TabsWithIDToBeRestored.Count == 0)
			{
				return false;
			}
			List<string> InstanceIDList = TabsWithIDToBeRestored
				.Select(x => x.InstanceID)
				.Distinct()
				.ToList();
			foreach (string InstanceID in InstanceIDList)
			{
				List<TabItemWithIDArguments> TabsWithIDToBeRestoredForInstance = TabsWithIDToBeRestored
					.Where(x => x.InstanceID == InstanceID)
					.ToList();
				List<CustomTabViewItemParameter> TabsToBeRestoredForInstance = TabsWithIDToBeRestoredForInstance
					.Select(x => x as CustomTabViewItemParameter)
					.ToList();
				List<string> TabsToBeRestoredForInstanceStr = TabsToBeRestoredForInstance
					.Select(x => x.Serialize())
					.ToList();
				NavigationHelpers.OpenTabsInNewWindowAsync(TabsToBeRestoredForInstanceStr);
			}
			return true;
		}
		public static bool RestoreLastAppsTabs(MainPageViewModel mainPageViewModel)
		{
			ReadSharedMemory();
			if (userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList is null)
			{
				return false;
			}
			List<TabItemWithIDArguments> LastAppsTabsWithIDArgList = userSettingsService.GeneralSettingsService.LastAppsTabsWithIDList
				.Select(x => TabItemWithIDArguments.Deserialize(x))
				.ToList();
			List<string> TabsIDList = TabsWithIDArgList
				.Select(x => x.InstanceID)
				.Distinct()
				.ToList();
			List<TabItemWithIDArguments> TabsWithIDToBeRestored = LastAppsTabsWithIDArgList
				.Where(x => !TabsIDList.Contains(x.InstanceID))
				.ToList();
			if (TabsWithIDToBeRestored.Count == 0)
			{
				return false;
			}
			List<string> InstanceIDList = TabsWithIDToBeRestored
				.Select(x => x.InstanceID)
				.Distinct()
				.ToList();
			for(int i = 0; i < InstanceIDList.Count; i++)
			{
				string InstanceID = InstanceIDList[i];
				List<TabItemWithIDArguments> TabsWithIDToBeRestoredForInstance = TabsWithIDToBeRestored
					.Where(x => x.InstanceID == InstanceID)
					.ToList();
				List<CustomTabViewItemParameter> TabsToBeRestoredForInstance = TabsWithIDToBeRestoredForInstance
					.Select(x => x as CustomTabViewItemParameter)
					.ToList();
				List<string> TabsToBeRestoredForInstanceStr = TabsToBeRestoredForInstance
					.Select(x => x.Serialize())
					.ToList();
				if (i == 0)
				{
					foreach (var tabArgs in TabsToBeRestoredForInstance)
					{
						mainPageViewModel.AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationParameter);
					}
				}
				else
				{
					NavigationHelpers.OpenTabsInNewWindowAsync(TabsToBeRestoredForInstanceStr);
				}
			}
			return true;
		}
	}


	public class TabItemWithIDArguments : CustomTabViewItemParameter
	{
		public string InstanceID { get; set; }
		private static readonly KnownTypesConverter TypesConverter = new KnownTypesConverter();

		public TabItemWithIDArguments()
		{
			InstanceID = AppLifecycle.InstanceID;
		}

		public new string Serialize()
		{
			var	TabArg = JsonSerializer.Serialize(this, TypesConverter.Options);
			//TabArg = "{*" + InstanceID+ "*}" + "{*" + TabArg + "*}";
			return TabArg;
		}

		public static new TabItemWithIDArguments Deserialize(string obj)
		{
			var tabArgs = new TabItemWithIDArguments();

			var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj);
			tabArgs.InitialPageType = Type.GetType(tempArgs["InitialPageType"].GetString());

			try
			{
				tabArgs.NavigationParameter = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs["NavigationParameter"].GetRawText());
			}
			catch (JsonException)
			{
				tabArgs.NavigationParameter = tempArgs["NavigationParameter"].GetString();
			}
			if (tempArgs.ContainsKey("InstanceID"))
			{
				tabArgs.InstanceID = tempArgs["InstanceID"].GetString();
			}
			else
			{
				tabArgs.InstanceID = AppLifecycle.InstanceID;
			}
			return tabArgs;
		}

		public static TabItemWithIDArguments CreateFromTabItemArg(CustomTabViewItemParameter tabItemArg)
		{
			string json = JsonSerializer.Serialize(tabItemArg);
			var tabItemWithIDArg = JsonSerializer.Deserialize<TabItemWithIDArguments>(json);
			tabItemWithIDArg.InstanceID = AppLifecycle.InstanceID;
			return tabItemWithIDArg;
		}

		public CustomTabViewItemParameter ExportToTabItemArg()
		{
			string json = JsonSerializer.Serialize(this);
			var tabItemArg = JsonSerializer.Deserialize<CustomTabViewItemParameter>(json);
			return tabItemArg;
		}
	}
}
