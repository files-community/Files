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
		private static EverythingSdk3Service _sdk3Service;

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
		
		[DllImport("Everything", EntryPoint = "Everything_SetSort")]
		private static extern void Everything_SetSort(uint dwSortType);
		
		[DllImport("Everything", EntryPoint = "Everything_IsFileResult")]
		private static extern bool Everything_IsFileResult(uint nIndex);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultPath", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything_GetResultPath(uint nIndex);
		
		[DllImport("Everything", EntryPoint = "Everything_GetResultFileName", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything_GetResultFileName(uint nIndex);
		
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
			// Try SDK3 first if available
			if (_sdk3Service == null)
			{
				try
				{
					_sdk3Service = new EverythingSdk3Service();
					if (_sdk3Service.Connect())
					{
						App.Logger?.LogInformation("[EverythingSizeProvider] Connected to Everything SDK3 for folder size calculation");
					}
					else
					{
						_sdk3Service?.Dispose();
						_sdk3Service = null;
					}
				}
				catch (Exception ex)
				{
					App.Logger?.LogWarning(ex, "[EverythingSizeProvider] SDK3 not available");
					_sdk3Service = null;
				}
			}
			
			if (_sdk3Service != null && _sdk3Service.IsConnected)
			{
				try
				{
					var size = await Task.Run(() => _sdk3Service.GetFolderSize(path), cancellationToken);
					if (size > 0)
					{
						App.Logger?.LogInformation($"[EverythingSizeProvider SDK3] Got folder size for {path}: {size} bytes");
						return size;
					}
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, $"[EverythingSizeProvider SDK3] Error getting folder size for {path}");
				}
			}
			
			// Fall back to SDK2
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
					
					// Use an optimized query that only returns files (not folders) to reduce result count
					// The folder: syntax searches recursively within the specified folder
					// Adding file: ensures we only get files, not directories
					var searchQuery = $"folder:\"{path}\" file:";
					Everything_SetSearchW(searchQuery);
					
					// Request only size information to optimize performance
					Everything_SetRequestFlags(EVERYTHING_REQUEST_SIZE);
					
					// Sort by size descending to prioritize large files if we hit the limit
					Everything_SetSort(13); // EVERYTHING_SORT_SIZE_DESCENDING
					
					// Use configurable max results limit
					var maxResults = (uint)generalSettings.EverythingMaxFolderSizeResults;
					Everything_SetMax(maxResults);
					
					queryExecuted = Everything_QueryW(true);
					if (!queryExecuted)
						return 0UL;

					var numResults = Everything_GetNumResults();
					
					// If we hit the limit, we're still getting the largest files first
					// This gives a more accurate estimate even with limited results
					if (numResults >= maxResults)
					{
						App.Logger?.LogInformation($"[EverythingSizeProvider SDK2] Hit result limit ({maxResults}) for {path}, results may be incomplete");
					}
					
					ulong totalSize = 0;
					int validResults = 0;

					for (uint i = 0; i < numResults; i++)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						if (Everything_GetResultSize(i, out long size) && size > 0)
						{
							totalSize += (ulong)size;
							validResults++;
						}
					}
					
					// If we got very few results or hit the limit for a folder that should have more files,
					// fall back to standard calculation
					if (numResults >= maxResults && validResults < 100)
					{
						App.Logger?.LogInformation($"[EverythingSizeProvider SDK2] Too few valid results ({validResults}) for {path}, using fallback");
						return 0UL; // Will trigger fallback calculation
					}

					App.Logger?.LogInformation($"[EverythingSizeProvider SDK2] Calculated {totalSize} bytes for {path} ({validResults} files)");
					return totalSize;
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, $"[EverythingSizeProvider SDK2] Error calculating with Everything for {path}");
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