// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Storage.Search
{
    /// <summary>
    /// Service for selecting the appropriate search engine based on user settings
    /// </summary>
    public interface ISearchEngineSelector
    {
        /// <summary>
        /// Gets the currently selected search engine service
        /// </summary>
        ISearchEngineService Current { get; }

        /// <summary>
        /// Gets the currently selected search engine service
        /// </summary>
        /// <returns>The active search engine service</returns>
        ISearchEngineService GetCurrentSearchEngine();

        /// <summary>
        /// Gets the search engine service by name
        /// </summary>
        /// <param name="name">The name of the search engine</param>
        /// <returns>The requested search engine service, or null if not found</returns>
        ISearchEngineService? GetSearchEngineByName(string name);

        /// <summary>
        /// Gets all available search engine services
        /// </summary>
        /// <returns>Collection of all search engine services</returns>
        IEnumerable<ISearchEngineService> GetAllSearchEngines();
    }
}
