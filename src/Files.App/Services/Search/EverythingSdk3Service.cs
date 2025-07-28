// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Files.App.Services.Search
{
	/// <summary>
	/// Everything SDK3 (v1.5) implementation for improved performance
	/// </summary>
	internal sealed class EverythingSdk3Service : IDisposable
	{
		#region SDK3 Definitions
		
		private const uint EVERYTHING3_OK = 0;
		private const uint EVERYTHING3_ERROR_OUT_OF_MEMORY = 0xE0000001;
		private const uint EVERYTHING3_ERROR_IPC_PIPE_NOT_FOUND = 0xE0000002;
		private const uint EVERYTHING3_ERROR_DISCONNECTED = 0xE0000003;
		private const uint EVERYTHING3_ERROR_INVALID_PARAMETER = 0xE0000004;
		private const uint EVERYTHING3_ERROR_BAD_REQUEST = 0xE0000005;
		private const uint EVERYTHING3_ERROR_CANCELLED = 0xE0000006;
		private const uint EVERYTHING3_ERROR_PROPERTY_NOT_FOUND = 0xE0000007;
		private const uint EVERYTHING3_ERROR_SERVER = 0xE0000008;
		private const uint EVERYTHING3_ERROR_INVALID_COMMAND = 0xE0000009;
		private const uint EVERYTHING3_ERROR_BAD_RESPONSE = 0xE000000A;
		private const uint EVERYTHING3_ERROR_INSUFFICIENT_BUFFER = 0xE000000B;
		private const uint EVERYTHING3_ERROR_SHUTDOWN = 0xE000000C;
		
		// Property IDs
		private const uint EVERYTHING3_PROPERTY_SIZE = 0x00000001;
		private const uint EVERYTHING3_PROPERTY_DATE_MODIFIED = 0x00000002;
		private const uint EVERYTHING3_PROPERTY_DATE_CREATED = 0x00000003;
		private const uint EVERYTHING3_PROPERTY_ATTRIBUTES = 0x00000004;
		private const uint EVERYTHING3_PROPERTY_PATH = 0x00000005;
		private const uint EVERYTHING3_PROPERTY_NAME = 0x00000006;
		private const uint EVERYTHING3_PROPERTY_EXTENSION = 0x00000007;
		private const uint EVERYTHING3_PROPERTY_TYPE_NAME = 0x00000008;
		
		// Result types
		private const uint EVERYTHING3_RESULT_TYPE_FILE = 1;
		private const uint EVERYTHING3_RESULT_TYPE_FOLDER = 2;
		
		#endregion
		
		#region P/Invoke Declarations
		
		[DllImport("Everything3", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything3_ConnectW(string instance_name);
		
		[DllImport("Everything3")]
		private static extern bool Everything3_DestroyClient(IntPtr client);
		
		[DllImport("Everything3")]
		private static extern bool Everything3_ShutdownClient(IntPtr client);
		
		[DllImport("Everything3", CharSet = CharSet.Unicode)]
		private static extern ulong Everything3_GetFolderSizeFromFilenameW(IntPtr client, string filename);
		
		[DllImport("Everything3", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything3_CreateQuery(IntPtr client, string search_string);
		
		[DllImport("Everything3")]
		private static extern bool Everything3_DestroyQuery(IntPtr query);
		
		[DllImport("Everything3")]
		private static extern bool Everything3_SetMax(IntPtr query, uint max);
		
		[DllImport("Everything3")]
		private static extern bool Everything3_SetOffset(IntPtr query, uint offset);
		
		[DllImport("Everything3")]
		private static extern bool Everything3_SetRequestProperties(IntPtr query, uint property_ids, uint property_count);
		
		[DllImport("Everything3")]
		private static extern bool Everything3_Execute(IntPtr query);
		
		[DllImport("Everything3")]
		private static extern uint Everything3_GetCount(IntPtr query);
		
		[DllImport("Everything3")]
		private static extern uint Everything3_GetResultType(IntPtr query, uint index);
		
		[DllImport("Everything3", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything3_GetResultPathW(IntPtr query, uint index);
		
		[DllImport("Everything3", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything3_GetResultNameW(IntPtr query, uint index);
		
		[DllImport("Everything3")]
		private static extern ulong Everything3_GetResultSize(IntPtr query, uint index);
		
		[DllImport("Everything3")]
		private static extern ulong Everything3_GetResultDateModified(IntPtr query, uint index);
		
		[DllImport("Everything3")]
		private static extern ulong Everything3_GetResultDateCreated(IntPtr query, uint index);
		
		[DllImport("Everything3")]
		private static extern uint Everything3_GetResultAttributes(IntPtr query, uint index);
		
		[DllImport("Everything3")]
		private static extern uint Everything3_GetLastError();
		
		#endregion
		
		private IntPtr _client;
		private readonly object _lock = new object();
		private bool _disposed;
		
		public bool IsConnected => _client != IntPtr.Zero;
		
		public bool Connect()
		{
			lock (_lock)
			{
				if (_client != IntPtr.Zero)
					return true;
				
				try
				{
					// Try to connect to unnamed instance first
					_client = Everything3_ConnectW(null);
					if (_client != IntPtr.Zero)
					{
						App.Logger?.LogInformation("[Everything SDK3] Connected to unnamed instance");
						return true;
					}
					
					// Try to connect to 1.5a instance
					_client = Everything3_ConnectW("1.5a");
					if (_client != IntPtr.Zero)
					{
						App.Logger?.LogInformation("[Everything SDK3] Connected to 1.5a instance");
						return true;
					}
					
					App.Logger?.LogWarning("[Everything SDK3] Failed to connect to Everything 1.5");
					return false;
				}
				catch (DllNotFoundException)
				{
					App.Logger?.LogInformation("[Everything SDK3] SDK3 DLL not found - Everything 1.5 not installed");
					return false;
				}
				catch (EntryPointNotFoundException)
				{
					App.Logger?.LogWarning("[Everything SDK3] SDK3 entry point not found - incompatible DLL");
					return false;
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, "[Everything SDK3] Error connecting to Everything 1.5");
					return false;
				}
			}
		}
		
		public ulong GetFolderSize(string folderPath)
		{
			if (!IsConnected || string.IsNullOrEmpty(folderPath))
				return 0;
			
			lock (_lock)
			{
				try
				{
					var size = Everything3_GetFolderSizeFromFilenameW(_client, folderPath);
					
					// Check for errors (-1 indicates error)
					if (size == ulong.MaxValue)
					{
						var error = Everything3_GetLastError();
						App.Logger?.LogWarning($"[Everything SDK3] GetFolderSize failed for {folderPath}, error: 0x{error:X8}");
						return 0;
					}
					
					App.Logger?.LogInformation($"[Everything SDK3] Got folder size for {folderPath}: {size} bytes");
					return size;
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, $"[Everything SDK3] Error getting folder size for {folderPath}");
					return 0;
				}
			}
		}
		
		public async Task<List<(string Path, string Name, bool IsFolder, ulong Size, DateTime DateModified, DateTime DateCreated, uint Attributes)>> SearchAsync(
			string searchQuery, 
			uint maxResults = 1000, 
			CancellationToken cancellationToken = default)
		{
			if (!IsConnected || string.IsNullOrEmpty(searchQuery))
				return new List<(string, string, bool, ulong, DateTime, DateTime, uint)>();
			
			return await Task.Run(() =>
			{
				lock (_lock)
				{
					IntPtr query = IntPtr.Zero;
					var results = new List<(string Path, string Name, bool IsFolder, ulong Size, DateTime DateModified, DateTime DateCreated, uint Attributes)>();
					
					try
					{
						query = Everything3_CreateQuery(_client, searchQuery);
						if (query == IntPtr.Zero)
						{
							App.Logger?.LogWarning("[Everything SDK3] Failed to create query");
							return results;
						}
						
						// Set max results
						Everything3_SetMax(query, maxResults);
						
						// Request properties we need
						uint[] properties = { 
							EVERYTHING3_PROPERTY_PATH,
							EVERYTHING3_PROPERTY_NAME,
							EVERYTHING3_PROPERTY_SIZE,
							EVERYTHING3_PROPERTY_DATE_MODIFIED,
							EVERYTHING3_PROPERTY_DATE_CREATED,
							EVERYTHING3_PROPERTY_ATTRIBUTES
						};
						
						GCHandle propertiesHandle = GCHandle.Alloc(properties, GCHandleType.Pinned);
						try
						{
							Everything3_SetRequestProperties(query, (uint)propertiesHandle.AddrOfPinnedObject(), (uint)properties.Length);
						}
						finally
						{
							propertiesHandle.Free();
						}
						
						// Execute query
						if (!Everything3_Execute(query))
						{
							var error = Everything3_GetLastError();
							App.Logger?.LogWarning($"[Everything SDK3] Query execution failed, error: 0x{error:X8}");
							return results;
						}
						
						var count = Everything3_GetCount(query);
						App.Logger?.LogInformation($"[Everything SDK3] Query returned {count} results");
						
						for (uint i = 0; i < count; i++)
						{
							if (cancellationToken.IsCancellationRequested)
								break;
							
							try
							{
								var type = Everything3_GetResultType(query, i);
								var isFolder = type == EVERYTHING3_RESULT_TYPE_FOLDER;
								
								var path = Marshal.PtrToStringUni(Everything3_GetResultPathW(query, i)) ?? string.Empty;
								var name = Marshal.PtrToStringUni(Everything3_GetResultNameW(query, i)) ?? string.Empty;
								var size = Everything3_GetResultSize(query, i);
								var dateModified = DateTime.FromFileTimeUtc((long)Everything3_GetResultDateModified(query, i));
								var dateCreated = DateTime.FromFileTimeUtc((long)Everything3_GetResultDateCreated(query, i));
								var attributes = Everything3_GetResultAttributes(query, i);
								
								results.Add((path, name, isFolder, size, dateModified, dateCreated, attributes));
							}
							catch (Exception ex)
							{
								App.Logger?.LogError(ex, $"[Everything SDK3] Error processing result {i}");
							}
						}
						
						return results;
					}
					catch (Exception ex)
					{
						App.Logger?.LogError(ex, "[Everything SDK3] Error during search");
						return results;
					}
					finally
					{
						if (query != IntPtr.Zero)
								Everything3_DestroyQuery(query);
					}
				}
			}, cancellationToken);
		}
		
		public void Dispose()
		{
			lock (_lock)
			{
				if (_disposed)
					return;
				
				if (_client != IntPtr.Zero)
				{
					try
					{
						Everything3_ShutdownClient(_client);
						Everything3_DestroyClient(_client);
					}
					catch (Exception ex)
					{
						App.Logger?.LogError(ex, "[Everything SDK3] Error during cleanup");
					}
					_client = IntPtr.Zero;
				}
				
				_disposed = true;
			}
		}
	}
}
