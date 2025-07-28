// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Models;
using Files.App.ViewModels;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Windows.Storage;
using Microsoft.Extensions.Logging;

namespace Files.App.Services.Search
{
	public sealed class EverythingSearchService : IEverythingSearchService
	{
		// Everything API constants
		private const int EVERYTHING_OK = 0;
		private const int EVERYTHING_ERROR_IPC = 2;
		
		private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
		private const int EVERYTHING_REQUEST_PATH = 0x00000002;
		private const int EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;
		private const int EVERYTHING_REQUEST_SIZE = 0x00000010;
		private const int EVERYTHING_REQUEST_DATE_CREATED = 0x00000020;
		private const int EVERYTHING_REQUEST_ATTRIBUTES = 0x00000100;

		// Architecture-aware DLL name
		private static readonly string EverythingDllName = GetArchitectureSpecificDllName();
		
		private static string GetArchitectureSpecificDllName()
		{
			// Check for ARM64 first
			if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
			{
				// Use native ARM64 DLL for better performance
				return "EverythingARM64.dll";
			}
			
			// Standard x86/x64 detection
			return Environment.Is64BitProcess ? "Everything64.dll" : "Everything32.dll";
		}

		// Everything API imports - using architecture-aware DLL resolution
		[DllImport("Everything", EntryPoint = "Everything_SetSearchW", CharSet = CharSet.Unicode)]
		private static extern uint Everything_SetSearchW(string lpSearchString);
		
		[DllImport("Everything", EntryPoint = "Everything_SetMatchPath")]
		private static extern void Everything_SetMatchPath(bool bEnable);
		
		[DllImport("Everything", EntryPoint = "Everything_SetMatchCase")]
		private static extern void Everything_SetMatchCase(bool bEnable);
		
		[DllImport("Everything", EntryPoint = "Everything_SetMatchWholeWord")]
		private static extern void Everything_SetMatchWholeWord(bool bEnable);
		
		[DllImport("Everything", EntryPoint = "Everything_SetRegex")]
		private static extern void Everything_SetRegex(bool bEnable);
		
		[DllImport("Everything", EntryPoint = "Everything_SetMax")]
		private static extern void Everything_SetMax(uint dwMax);
		
		[DllImport("Everything", EntryPoint = "Everything_SetOffset")]
		private static extern void Everything_SetOffset(uint dwOffset);
		
		[DllImport("Everything", EntryPoint = "Everything_SetRequestFlags")]
		private static extern void Everything_SetRequestFlags(uint dwRequestFlags);
		
		[DllImport("Everything", EntryPoint = "Everything_QueryW")]
		private static extern bool Everything_QueryW(bool bWait);
		
		[DllImport("Everything", EntryPoint = "Everything_GetNumResults")]
		private static extern uint Everything_GetNumResults();
		
		[DllImport("Everything", EntryPoint = "Everything_GetLastError")]
		private static extern uint Everything_GetLastError();
		
		[DllImport("Everything", EntryPoint = "Everything_IsFileResult")]
		private static extern bool Everything_IsFileResult(uint nIndex);
		
		[DllImport("Everything", EntryPoint = "Everything_IsFolderResult")]
		private static extern bool Everything_IsFolderResult(uint nIndex);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultPath", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything_GetResultPath(uint nIndex);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultFileName", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything_GetResultFileName(uint nIndex);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultDateModified")]
		private static extern bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultDateCreated")]
		private static extern bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultSize")]
		private static extern bool Everything_GetResultSize(uint nIndex, out long lpFileSize);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultAttributes")]
		private static extern uint Everything_GetResultAttributes(uint nIndex);
		
		[DllImport("Everything", EntryPoint = "Everything_Reset")]
		private static extern void Everything_Reset();
		
		[DllImport("Everything", EntryPoint = "Everything_SetSort")]
		private static extern void Everything_SetSort(uint dwSortType);
		
		// Note: Everything_CleanUp is intentionally not imported as it can cause access violations
		// Everything_Reset() is sufficient for resetting the query state between searches
		
		[DllImport("Everything", EntryPoint = "Everything_IsDBLoaded")]
		private static extern bool Everything_IsDBLoaded();

		// Win32 API imports for DLL loading
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadLibrary(string lpLibFileName);
		
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeLibrary(IntPtr hLibModule);
		
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool SetDllDirectory(string lpPathName);
		
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr AddDllDirectory(string newDirectory);
		
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool RemoveDllDirectory(IntPtr cookie);

		private readonly IUserSettingsService _userSettingsService;
		private static readonly object _dllSetupLock = new object();
		private static IntPtr _everythingModule = IntPtr.Zero;
		private static readonly List<IntPtr> _dllDirectoryCookies = new();
		private static bool _dllDirectorySet = false;
		private static bool _everythingAvailable = false;
		private static bool _availabilityChecked = false;
		private static DateTime _lastAvailabilityCheck = default;
		
		// SDK3 support
		private static EverythingSdk3Service _sdk3Service;
		private static bool _sdk3Available = false;
		private static bool _sdk3Checked = false;

		static EverythingSearchService()
		{
			// Set up DLL import resolver for architecture-aware loading
			NativeLibrary.SetDllImportResolver(typeof(EverythingSearchService).Assembly, DllImportResolver);
		}

		public EverythingSearchService(IUserSettingsService userSettingsService)
		{
			_userSettingsService = userSettingsService;
			
			// Set up DLL search path if not already done
			lock (_dllSetupLock)
			{
				if (!_dllDirectorySet)
				{
					SetupDllSearchPath();
					_dllDirectorySet = true;
				}
			}
		}

		private static IntPtr DllImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
		{
			if (libraryName == "Everything")
			{
				lock (_dllSetupLock)
				{
					if (_everythingModule != IntPtr.Zero)
					{
						return _everythingModule;
					}


					// Try to load Everything DLL from various locations
					var appDirectory = AppContext.BaseDirectory;
					var possiblePaths = new[]
					{
						Path.Combine(appDirectory, "Libraries", EverythingDllName),
						Path.Combine(appDirectory, EverythingDllName),
						Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything", EverythingDllName),
						Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything", EverythingDllName),
						Path.Combine(@"C:\Program Files\Everything", EverythingDllName),
						Path.Combine(@"C:\Program Files (x86)\Everything", EverythingDllName),
						EverythingDllName // Try system path
					};

					foreach (var path in possiblePaths)
					{
						if (File.Exists(path))
						{
							try
							{
								_everythingModule = LoadLibrary(path);
								if (_everythingModule != IntPtr.Zero)
								{
									return _everythingModule;
								}
								else
								{
									var error = Marshal.GetLastWin32Error();
								}
							}
							catch (Exception ex)
							{
							}
						}
						else
						{
						}
					}

					// If not found, let the default resolver handle it
					return IntPtr.Zero;
				}
			}

			// Use default resolver for other libraries
			return NativeLibrary.Load(libraryName, assembly, searchPath);
		}

		private static void SetupDllSearchPath()
		{
			try
			{
				// Get the application directory
				var appDirectory = AppContext.BaseDirectory;
				var searchPaths = new[]
				{
					appDirectory,
					Path.Combine(appDirectory, "Libraries"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything")
				};

				foreach (var path in searchPaths)
				{
					if (Directory.Exists(path))
					{
						var cookie = AddDllDirectory(path);
						if (cookie != IntPtr.Zero)
						{
							_dllDirectoryCookies.Add(cookie);
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error setting up DLL search paths: {ex.Message}");
			}
		}

		public static void CleanupDllDirectories()
		{
			lock (_dllSetupLock)
			{
				foreach (var cookie in _dllDirectoryCookies)
				{
					RemoveDllDirectory(cookie);
				}
				_dllDirectoryCookies.Clear();

				if (_everythingModule != IntPtr.Zero)
				{
					FreeLibrary(_everythingModule);
					_everythingModule = IntPtr.Zero;
				}
			}
		}

		public bool IsEverythingAvailable()
		{
			lock (_dllSetupLock)
			{
				// Re-check availability every 30 seconds to detect if Everything is started/stopped
				if (_availabilityChecked && _lastAvailabilityCheck != default && 
				    DateTime.UtcNow - _lastAvailabilityCheck < TimeSpan.FromSeconds(30))
				{
					return _everythingAvailable || _sdk3Available;
				}
				
				// Check SDK3 first (Everything 1.5)
				// Note: SDK3 DLLs are not included and must be obtained separately from:
				// https://github.com/voidtools/everything_sdk3
				if (!_sdk3Checked)
				{
					try
					{
						if (_sdk3Service == null)
							_sdk3Service = new EverythingSdk3Service();
						
						_sdk3Available = _sdk3Service.Connect();
						_sdk3Checked = true;
						
						if (_sdk3Available)
						{
							App.Logger?.LogInformation("[Everything] Everything SDK3 (v1.5) is available");
							_lastAvailabilityCheck = DateTime.UtcNow;
							return true;
						}
					}
					catch (Exception ex)
					{
						App.Logger?.LogWarning(ex, "[Everything] SDK3 not available, falling back to SDK2");
						_sdk3Available = false;
						_sdk3Checked = true;
					}
				}

				try
				{
					// First check if Everything process is running
					var everythingProcesses = System.Diagnostics.Process.GetProcessesByName("Everything");
					
					if (everythingProcesses.Length == 0)
					{
						App.Logger?.LogInformation("[Everything] Everything process not found - Everything is not running");
						_everythingAvailable = false;
						_availabilityChecked = true;
						_lastAvailabilityCheck = DateTime.UtcNow;
						return false;
					}

					// Try to perform a simple query to test if Everything is accessible
					bool queryExecuted = false;
					try
					{
						Everything_Reset();
						Everything_SetSearchW("test");
						Everything_SetMax(1);
						
						queryExecuted = Everything_QueryW(false);
						var lastError = Everything_GetLastError();
						
						_everythingAvailable = queryExecuted && lastError == EVERYTHING_OK;
						_availabilityChecked = true;
						_lastAvailabilityCheck = DateTime.UtcNow;

						if (_everythingAvailable)
						{
							App.Logger?.LogInformation("[Everything] Everything SDK2 (v1.4) is available and responding");
						}
						else
						{
							App.Logger?.LogWarning($"[Everything] Everything is not available. Query result: {queryExecuted}, Error: {lastError}");
						}
					}
					finally
					{
						// Note: Not calling Everything_CleanUp() to avoid access violations
						// Everything_Reset() will be called on the next query which handles cleanup
					}

					return _everythingAvailable;
				}
				catch (Exception ex)
				{
					_everythingAvailable = false;
					_availabilityChecked = true;
					_lastAvailabilityCheck = DateTime.UtcNow;
					return false;
				}
			}
		}

		public async Task<List<ListedItem>> SearchAsync(string query, string searchPath = null, CancellationToken cancellationToken = default)
		{
			if (!IsEverythingAvailable())
			{
				return new List<ListedItem>();
			}
			
			// Try SDK3 first if available
			if (_sdk3Available && _sdk3Service != null)
			{
				try
				{
					var searchQuery = BuildOptimizedQuery(query, searchPath);
					App.Logger?.LogInformation($"[Everything SDK3] Executing search query: '{searchQuery}'");
					
					var sdk3Results = await _sdk3Service.SearchAsync(searchQuery, 1000, cancellationToken);
					var results = new List<ListedItem>();
					
					foreach (var (path, name, isFolder, size, dateModified, dateCreated, attributes) in sdk3Results)
					{
						if (cancellationToken.IsCancellationRequested)
							break;
						
						var fullPath = string.IsNullOrEmpty(path) ? name : Path.Combine(path, name);
						
						// Skip if it doesn't match our filter criteria
						if (!string.IsNullOrEmpty(searchPath) && searchPath != "Home" && 
							!fullPath.StartsWith(searchPath, StringComparison.OrdinalIgnoreCase))
							continue;
						
						var isHidden = (attributes & 0x02) != 0; // FILE_ATTRIBUTE_HIDDEN
						
						// Check user settings for hidden items
						if (isHidden && !_userSettingsService.FoldersSettingsService.ShowHiddenItems)
							continue;
						
						// Check for dot files
						if (name.StartsWith('.') && !_userSettingsService.FoldersSettingsService.ShowDotFiles)
							continue;
						
						var item = new ListedItem(null)
						{
							PrimaryItemAttribute = isFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
							ItemNameRaw = name,
							ItemPath = fullPath,
							ItemDateModifiedReal = dateModified,
							ItemDateCreatedReal = dateCreated,
							IsHiddenItem = isHidden,
							LoadFileIcon = false,
							FileExtension = isFolder ? null : Path.GetExtension(fullPath),
							FileSizeBytes = isFolder ? 0 : (long)size,
							FileSize = isFolder ? null : ByteSizeLib.ByteSize.FromBytes(size).ToBinaryString(),
							Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
						};
						
						if (!isFolder)
						{
							item.ItemType = item.FileExtension?.Trim('.') + " " + Strings.File.GetLocalizedResource();
						}
						
						results.Add(item);
					}
					
					App.Logger?.LogInformation($"[Everything SDK3] Search completed with {results.Count} results");
					return results;
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, "[Everything SDK3] Search failed, falling back to SDK2");
					// Fall through to SDK2
				}
			}

			// SDK2 fallback
			return await Task.Run(() =>
			{
				var results = new List<ListedItem>();
				bool queryExecuted = false;

				try
				{
					Everything_Reset();

					// Set up the search query
					var searchQuery = BuildOptimizedQuery(query, searchPath);
					Everything_SetSearchW(searchQuery);
					Everything_SetMatchCase(false);
					Everything_SetRequestFlags(
						EVERYTHING_REQUEST_FILE_NAME | 
						EVERYTHING_REQUEST_PATH | 
						EVERYTHING_REQUEST_DATE_MODIFIED | 
						EVERYTHING_REQUEST_DATE_CREATED |
						EVERYTHING_REQUEST_SIZE |
						EVERYTHING_REQUEST_ATTRIBUTES);

					// Limit results to prevent overwhelming the UI
					Everything_SetMax(1000);

					// Execute the query
					App.Logger?.LogInformation($"[Everything SDK2] Executing search query: '{searchQuery}'");
					queryExecuted = Everything_QueryW(true);
					if (!queryExecuted)
					{
						var error = Everything_GetLastError();
						if (error == EVERYTHING_ERROR_IPC)
						{
							return results;
						}
						else
						{
							return results;
						}
					}

					var numResults = Everything_GetNumResults();
					App.Logger?.LogInformation($"[Everything SDK2] Query returned {numResults} results");

					for (uint i = 0; i < numResults; i++)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						try
						{
							var fileName = Marshal.PtrToStringUni(Everything_GetResultFileName(i));
							var path = Marshal.PtrToStringUni(Everything_GetResultPath(i));
							
							if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(path))
								continue;

							var fullPath = Path.Combine(path, fileName);

							// Skip if it doesn't match our filter criteria
							if (!string.IsNullOrEmpty(searchPath) && searchPath != "Home" && 
								!fullPath.StartsWith(searchPath, StringComparison.OrdinalIgnoreCase))
								continue;

							var isFolder = Everything_IsFolderResult(i);
							var attributes = Everything_GetResultAttributes(i);
							var isHidden = (attributes & 0x02) != 0; // FILE_ATTRIBUTE_HIDDEN

							// Check user settings for hidden items
							if (isHidden && !_userSettingsService.FoldersSettingsService.ShowHiddenItems)
								continue;

							// Check for dot files
							if (fileName.StartsWith('.') && !_userSettingsService.FoldersSettingsService.ShowDotFiles)
								continue;

							Everything_GetResultDateModified(i, out long dateModified);
							Everything_GetResultDateCreated(i, out long dateCreated);
							Everything_GetResultSize(i, out long size);

							var item = new ListedItem(null)
							{
								PrimaryItemAttribute = isFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
								ItemNameRaw = fileName,
								ItemPath = fullPath,
								ItemDateModifiedReal = DateTime.FromFileTime(dateModified),
								ItemDateCreatedReal = DateTime.FromFileTime(dateCreated),
								IsHiddenItem = isHidden,
								LoadFileIcon = false,
								FileExtension = isFolder ? null : Path.GetExtension(fullPath),
								FileSizeBytes = isFolder ? 0 : size,
								FileSize = isFolder ? null : ByteSizeLib.ByteSize.FromBytes((ulong)size).ToBinaryString(),
								Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
							};

							if (!isFolder)
							{
								item.ItemType = item.FileExtension?.Trim('.') + " " + Strings.File.GetLocalizedResource();
							}

							results.Add(item);
						}
						catch (Exception ex)
						{
							// Skip items that cause errors
							System.Diagnostics.Debug.WriteLine($"Error processing Everything result {i}: {ex.Message}");
						}
					}
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, "[Everything SDK2] Search error");
				}
				finally
				{
					// Note: We're not calling Everything_CleanUp() here as it can cause access violations
					// The Everything API manages its own memory and calling CleanUp can interfere with
					// the API's internal state, especially when multiple queries are executed in sequence
					// Everything_Reset() at the start of each query is sufficient for cleanup
				}

				return results;
			}, cancellationToken);
		}

		private string BuildOptimizedQuery(string query, string searchPath)
		{
			if (string.IsNullOrEmpty(searchPath) || searchPath == "Home")
			{
				return query;
			}
			else if (searchPath.Length <= 3) // Root drive like C:\
			{
				return $"path:\"{searchPath}\" {query}";
			}
			else
			{
				var escapedPath = searchPath.Replace("\"", "\"\"");
				return $"path:\"{escapedPath}\" {query}";
			}
		}

		public async Task<List<ListedItem>> FilterItemsAsync(IEnumerable<ListedItem> items, string query, CancellationToken cancellationToken = default)
		{
			// For filtering existing items, we'll use Everything's search on the current directory
			var firstItem = items.FirstOrDefault();
			if (firstItem == null)
				return new List<ListedItem>();

			// Get the directory path from the first item
			var directoryPath = Path.GetDirectoryName(firstItem.ItemPath);
			
			// Search within this directory
			var searchResults = await SearchAsync(query, directoryPath, cancellationToken);
			
			// Return only items that exist in the original collection
			var itemPaths = new HashSet<string>(items.Select(i => i.ItemPath), StringComparer.OrdinalIgnoreCase);
			return searchResults.Where(r => itemPaths.Contains(r.ItemPath)).ToList();
		}
	}
}