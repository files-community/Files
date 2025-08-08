// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils;
using Files.App.Utils.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Utils.Storage.Search
{
	public sealed class WindowsSearchEngineService : ISearchEngineService
	{
		public string Name => "Windows Search";

		public bool IsAvailable => true; // Windows Search is generally available

		/// <summary>
		/// Builds an optimized query string for Windows Search with optional path scoping
		/// </summary>
		/// <param name="query">The search query</param>
		/// <param name="searchPath">Optional path to scope the search to. Pass null for global search.</param>
		/// <returns>The query (Windows Search handles path scoping internally via FolderSearch)</returns>
		public string BuildOptimizedQuery(string query, string? searchPath)
		{
			// For Windows Search, FolderSearch handles path scoping internally
			// so we just return the original query
			return query ?? string.Empty;
		}

	public async Task<IList<ListedItem>> SearchAsync(string query, string? path, CancellationToken ct)
	{
		App.Logger.LogInformation("[SearchEngine: Windows Search] Starting search - Query: '{Query}', Path: '{Path}'", query, path ?? "<global>");
		
		// Handle the path scoping logic to match EverythingSearchEngineService behavior
		var searchPath = GetSearchPath(path);
		App.Logger.LogDebug("[SearchEngine: Windows Search] Resolved search path: '{SearchPath}'", searchPath ?? "<global>");
		
		var folderSearch = new FolderSearch
		{
			Query = query,
			Folder = searchPath
		};

		var results = new List<ListedItem>();
		await folderSearch.SearchAsync(results, ct);
		
		App.Logger.LogInformation("[SearchEngine: Windows Search] Search completed - Found {ResultCount} results", results.Count);
		return results;
	}

	public async Task<IList<ListedItem>> SuggestAsync(string query, string? path, CancellationToken ct)
	{
		App.Logger.LogInformation("[SearchEngine: Windows Search] Starting suggestions - Query: '{Query}', Path: '{Path}'", query, path ?? "<global>");
		
		// Handle the path scoping logic to match EverythingSearchEngineService behavior
		var searchPath = GetSearchPath(path);
		App.Logger.LogDebug("[SearchEngine: Windows Search] Resolved search path for suggestions: '{SearchPath}'", searchPath ?? "<global>");
		
		var folderSearch = new FolderSearch
		{
			Query = query,
			Folder = searchPath,
			MaxItemCount = 10 // Limit suggestions to reasonable number
		};

		var results = new List<ListedItem>();
		await folderSearch.SearchAsync(results, ct);
		
		App.Logger.LogInformation("[SearchEngine: Windows Search] Suggestions completed - Found {ResultCount} results", results.Count);
		return results;
	}

	/// <summary>
	/// Gets the appropriate search path, handling global search by passing null
	/// </summary>
	/// <param name="path">The requested search path</param>
	/// <returns>The path to use for FolderSearch, or null for global search</returns>
	private string? GetSearchPath(string? path)
	{
		// If path is null or empty, return null for global search
		if (string.IsNullOrEmpty(path))
			return null;

		// Return the path as-is - FolderSearch will handle Home, library paths, etc.
		return path;
	}
    }
}
