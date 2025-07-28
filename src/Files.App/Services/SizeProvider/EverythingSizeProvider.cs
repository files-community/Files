// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.Search;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Services.SizeProvider
{
	public sealed class EverythingSizeProvider : ISizeProvider
	{
		private readonly ConcurrentDictionary<string, ulong> sizes = new();
		private readonly IEverythingSearchService everythingService;
		private readonly IGeneralSettingsService generalSettings;

		public event EventHandler<SizeChangedEventArgs>? SizeChanged;

		// Everything API imports for folder size calculation
		[DllImport("Everything", EntryPoint = "Everything_SetSearchW", CharSet = CharSet.Unicode)]
		private static extern uint Everything_SetSearchW(string lpSearchString);
		
		[DllImport("Everything", EntryPoint = "Everything_SetRequestFlags")]
		private static extern void Everything_SetRequestFlags(uint dwRequestFlags);
		
		[DllImport("Everything", EntryPoint = "Everything_SetMax")]
		private static extern void Everything_SetMax(uint dwMax);
		
		[DllImport("Everything", EntryPoint = "Everything_QueryW")]
		private static extern bool Everything_QueryW(bool bWait);
		
		[DllImport("Everything", EntryPoint = "Everything_GetNumResults")]
		private static extern uint Everything_GetNumResults();
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultSize")]
		private static extern bool Everything_GetResultSize(uint nIndex, out long lpFileSize);
		
		[DllImport("Everything", EntryPoint = "Everything_Reset")]
		private static extern void Everything_Reset();
		
		// Note: Everything_CleanUp is intentionally not imported as it can cause access violations
		// Everything_Reset() is sufficient for resetting the query state between searches

		private const int EVERYTHING_REQUEST_SIZE = 0x00000010;

		public EverythingSizeProvider(IEverythingSearchService everythingSearchService, IGeneralSettingsService generalSettingsService)
		{
			everythingService = everythingSearchService;
			generalSettings = generalSettingsService;
		}

		public Task CleanAsync() => Task.CompletedTask;

		public Task ClearAsync()
		{
			sizes.Clear();
			return Task.CompletedTask;
		}

		public async Task UpdateAsync(string path, CancellationToken cancellationToken)
		{
			await Task.Yield();
			
			
			// Return cached size immediately if available
			if (sizes.TryGetValue(path, out ulong cachedSize))
			{
				RaiseSizeChanged(path, cachedSize, SizeChangedValueState.Final);
			}
			else
			{
				RaiseSizeChanged(path, 0, SizeChangedValueState.None);
			}

			// Check if Everything is available
			if (!everythingService.IsEverythingAvailable())
			{
				await FallbackCalculateAsync(path, cancellationToken);
				return;
			}

			try
			{
				// Calculate using Everything
				ulong totalSize = await CalculateWithEverythingAsync(path, cancellationToken);
				
				if (totalSize == 0)
				{
					// Everything returned 0, use fallback
					await FallbackCalculateAsync(path, cancellationToken);
					return;
				}
				sizes[path] = totalSize;
				RaiseSizeChanged(path, totalSize, SizeChangedValueState.Final);
			}
			catch (Exception ex)
			{
				// Fall back to standard calculation on error
				await FallbackCalculateAsync(path, cancellationToken);
			}
		}

		private async Task<ulong> CalculateWithEverythingAsync(string path, CancellationToken cancellationToken)
		{
			return await Task.Run(() =>
			{
				bool queryExecuted = false;
				try
				{
					// IMPORTANT: For large directories like C:\, this query can return millions of results
					// causing Everything to run out of memory. For root drives, fall back to standard calculation.
					if (path.Length <= 3 && path.EndsWith(":\\"))
					{
						return 0UL; // Will trigger fallback calculation
					}
					
					// For large known directories, also skip Everything
					var knownLargePaths = new[] { 
						@"C:\Windows", 
						@"C:\Program Files", 
						@"C:\Program Files (x86)",
						@"C:\Users",
						@"C:\ProgramData",
						@"C:\$Recycle.Bin",
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
					};
					
					if (knownLargePaths.Any(largePath => 
						string.Equals(path, largePath, StringComparison.OrdinalIgnoreCase) ||
						string.Equals(Path.GetFullPath(path), Path.GetFullPath(largePath), StringComparison.OrdinalIgnoreCase)))
					{
						return 0UL; // Will trigger fallback calculation
					}
					
					// Reset Everything state
					Everything_Reset();
					
					// Search for all files under this path
					// Use folder: to search within specific folder
					var searchQuery = $"folder:\"{path}\"";
					Everything_SetSearchW(searchQuery);
					Everything_SetRequestFlags(EVERYTHING_REQUEST_SIZE);
					
					// Use configurable max results limit
					var maxResults = (uint)generalSettings.EverythingMaxFolderSizeResults;
					Everything_SetMax(maxResults);
					
					queryExecuted = Everything_QueryW(true);
					if (!queryExecuted)
						return 0UL;

					var numResults = Everything_GetNumResults();
					
					// If we hit the limit, fall back to standard calculation
					if (numResults >= maxResults)
					{
						return 0UL; // Will trigger fallback calculation
					}
					
					ulong totalSize = 0;

					for (uint i = 0; i < numResults; i++)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						if (Everything_GetResultSize(i, out long size))
						{
							totalSize += (ulong)size;
						}
					}

					return totalSize;
				}
				catch (Exception ex)
				{
					return 0UL;
				}
				finally
				{
					// Note: We're not calling Everything_CleanUp() here as it can cause access violations
					// The Everything API manages its own memory and calling CleanUp can interfere with
					// the API's internal state, especially when multiple queries are executed in sequence
					// Everything_Reset() at the start of each query is sufficient for cleanup
				}
			}, cancellationToken);
		}

		private async Task FallbackCalculateAsync(string path, CancellationToken cancellationToken)
		{
			// Fallback to directory enumeration if Everything is not available
			ulong size = await CalculateRecursive(path, cancellationToken);
			sizes[path] = size;
			RaiseSizeChanged(path, size, SizeChangedValueState.Final);

			async Task<ulong> CalculateRecursive(string currentPath, CancellationToken ct, int level = 0)
			{
				if (string.IsNullOrEmpty(currentPath))
					return 0;

				ulong totalSize = 0;

				try
				{
					var directory = new DirectoryInfo(currentPath);
					
					// Get files in current directory
					foreach (var file in directory.GetFiles())
					{
						if (ct.IsCancellationRequested)
							break;
							
						totalSize += (ulong)file.Length;
					}

					// Recursively process subdirectories
					foreach (var subDirectory in directory.GetDirectories())
					{
						if (ct.IsCancellationRequested)
							break;

						// Skip symbolic links and junctions
						if ((subDirectory.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
							continue;

						var subDirSize = await CalculateRecursive(subDirectory.FullName, ct, level + 1);
						totalSize += subDirSize;
					}

					// Update intermediate results for top-level calculation
					// Note: Removed stopwatch tracking for simplicity after logging removal
				}
				catch (UnauthorizedAccessException)
				{
					// Skip directories we can't access
				}
				catch (DirectoryNotFoundException)
				{
					// Directory was deleted during enumeration
				}

				return totalSize;
			}
		}

		public bool TryGetSize(string path, out ulong size) => sizes.TryGetValue(path, out size);

		public void Dispose() { }

		private void RaiseSizeChanged(string path, ulong newSize, SizeChangedValueState valueState)
			=> SizeChanged?.Invoke(this, new SizeChangedEventArgs(path, newSize, valueState));
	}
}