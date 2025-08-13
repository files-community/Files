// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils;
using Files.App.Helpers.Application;
using Files.App.Services.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Extensions.Logging;
using FileAttributes = System.IO.FileAttributes;
using WIN32_FIND_DATA = Files.App.Helpers.Win32PInvoke.WIN32_FIND_DATA;

namespace Files.App.Utils.Storage.Search
{
	public sealed class EverythingSearchEngineService : ISearchEngineService
	{
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IEverythingSearchService _everythingService = Ioc.Default.GetRequiredService<IEverythingSearchService>();
		private const int MaxSuggestionResults = 10;
		private const int MaxSearchResults = 1000;
		
		// State for fallback notification (show only once per session)
		private static bool _hasNotifiedEverythingUnavailable = false;
		private readonly object _notificationLock = new object();

		public string Name => "Everything";

		public bool IsAvailable => _everythingService.IsEverythingAvailable();

		public async Task<IList<ListedItem>> SearchAsync(string query, string? path, CancellationToken ct)
		{
			App.Logger?.LogInformation("[SearchEngine: Everything] Starting search - Query: '{Query}', Path: '{Path}'", query, path ?? "<global>");
			
			if (!IsAvailable)
			{
				App.Logger?.LogWarning("[SearchEngine: Everything] Everything search unavailable, performing fallback to Windows Search");
				await NotifyEverythingUnavailableOnce();
				return await FallbackToWindowsSearch(query, path, MaxSearchResults, ct);
			}

			try
			{
				var results = await _everythingService.SearchAsync(query, path, ct);
				App.Logger?.LogInformation("[SearchEngine: Everything] Search completed - Found {ResultCount} results", results.Count);
				return results;
			}
			catch (Exception ex)
			{
				App.Logger?.LogError(ex, "[SearchEngine: Everything] Search failed, falling back to Windows Search");
				return await FallbackToWindowsSearch(query, path, MaxSearchResults, ct);
			}
		}

		public async Task<IList<ListedItem>> SuggestAsync(string query, string? path, CancellationToken ct)
		{
			App.Logger?.LogInformation("[SearchEngine: Everything] Starting suggestions - Query: '{Query}', Path: '{Path}'", query, path ?? "<global>");
			
			if (!IsAvailable)
			{
				App.Logger?.LogWarning("[SearchEngine: Everything] Everything search unavailable, performing fallback to Windows Search for suggestions");
				await NotifyEverythingUnavailableOnce();
				return await FallbackToWindowsSearch(query, path, MaxSuggestionResults, ct);
			}

			try
			{
				// Use Everything API with limited results for suggestions
				var results = await _everythingService.SearchAsync(query, path, ct);
				// Limit to suggestion count
				var limitedResults = results.Take(MaxSuggestionResults).ToList();
				App.Logger?.LogInformation("[SearchEngine: Everything] Suggestions completed - Found {ResultCount} results", limitedResults.Count);
				return limitedResults;
			}
			catch (Exception ex)
			{
				App.Logger?.LogError(ex, "[SearchEngine: Everything] Suggestions failed, falling back to Windows Search");
				return await FallbackToWindowsSearch(query, path, MaxSuggestionResults, ct);
			}
		}

		/// <summary>
		/// Notifies user once per session that Everything is unavailable and Windows Search fallback is being used
		/// </summary>
		private async Task NotifyEverythingUnavailableOnce()
		{
			lock (_notificationLock)
			{
				if (_hasNotifiedEverythingUnavailable)
					return;
					
				_hasNotifiedEverythingUnavailable = true;
			}
			
			try
			{
				App.Logger?.LogInformation("[SearchEngine: Everything] Showing fallback notification to user");
				
				// Everything is not available - search will use Windows Search instead
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "[SearchEngine: Everything] Failed to show fallback notification");
			}
		}
		
		/// <summary>
		/// Fallback to Windows Search when Everything is unavailable
		/// </summary>
		private async Task<IList<ListedItem>> FallbackToWindowsSearch(string query, string? path, int maxResults, CancellationToken ct)
		{
			try
			{
				App.Logger?.LogInformation("[SearchEngine: Everything] Falling back to Windows Search");
				
				// Use Windows Search service as fallback
				var windowsSearchService = Ioc.Default.GetRequiredService<WindowsSearchEngineService>();
				var results = maxResults == MaxSuggestionResults 
					? await windowsSearchService.SuggestAsync(query, path, ct)
					: await windowsSearchService.SearchAsync(query, path, ct);
					
				App.Logger?.LogInformation("[SearchEngine: Everything] Windows Search fallback completed - Found {ResultCount} results", results.Count);
				return results;
			}
			catch (Exception ex)
			{
				App.Logger?.LogError(ex, "[SearchEngine: Everything] Windows Search fallback failed");
				return new List<ListedItem>();
			}
		}
	}
}