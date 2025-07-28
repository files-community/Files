// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Data.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Files.App.Utils.Storage.Search
{
    /// <summary>
    /// Service for selecting the appropriate search engine based on user settings and availability
    /// </summary>
    public sealed class SearchEngineSelector : ISearchEngineSelector
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserSettingsService _userSettingsService;
        private readonly List<ISearchEngineService> _searchEngines;

        public SearchEngineSelector(IServiceProvider serviceProvider, IUserSettingsService userSettingsService)
        {
            _serviceProvider = serviceProvider;
            _userSettingsService = userSettingsService;
            
            // Get all search engine service instances
            _searchEngines = new List<ISearchEngineService>
            {
                _serviceProvider.GetRequiredService<WindowsSearchEngineService>(),
                _serviceProvider.GetRequiredService<EverythingSearchEngineService>()
            };
        }

        /// <inheritdoc/>
        public ISearchEngineService Current => GetCurrentSearchEngine();

        /// <inheritdoc/>
        public ISearchEngineService GetCurrentSearchEngine()
        {
            try
            {
                App.Logger.LogDebug("[SearchEngineSelector] Determining current search engine");
                
                // Get user's preferred search engine from settings
                // For now, we'll use a simple property check (this would need to be added to user settings)
                // In a real implementation, you'd have a setting like:
                // var preferredEngine = _userSettingsService.GeneralSettingsService.PreferredSearchEngine;
                
                // Fallback logic: Try to get preferred engine by name, fallback to available engines
                var preferredEngineName = GetPreferredSearchEngineName();
                App.Logger.LogDebug("[SearchEngineSelector] Preferred engine: '{PreferredEngine}'", preferredEngineName);
                
                // First, try to get the preferred engine if it's available
                var preferredEngine = _searchEngines.FirstOrDefault(engine => 
                    engine.Name.Equals(preferredEngineName, StringComparison.OrdinalIgnoreCase) && engine.IsAvailable);
                
                if (preferredEngine != null)
                {
                    App.Logger.LogInformation("[SearchEngineSelector] Using preferred search engine: '{EngineName}'", preferredEngine.Name);
                    return preferredEngine;
                }
                
                App.Logger.LogWarning("[SearchEngineSelector] Preferred engine '{PreferredEngine}' not available, falling back", preferredEngineName);
                
                // Fallback to first available engine (Windows Search should always be available)
                var fallbackEngine = _searchEngines.FirstOrDefault(engine => engine.IsAvailable);
                
                if (fallbackEngine != null)
                {
                    App.Logger.LogInformation("[SearchEngineSelector] Using fallback search engine: '{EngineName}'", fallbackEngine.Name);
                    return fallbackEngine;
                }
                
                // If no engines are available, return Windows Search as final fallback
                var finalFallback = _searchEngines.First(engine => engine is WindowsSearchEngineService);
                App.Logger.LogWarning("[SearchEngineSelector] No engines available, using final fallback: '{EngineName}'", finalFallback.Name);
                return finalFallback;
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "[SearchEngineSelector] Error determining current search engine, falling back to Windows Search");
                return _searchEngines.First(engine => engine is WindowsSearchEngineService);
            }
        }

        /// <inheritdoc/>
        public ISearchEngineService? GetSearchEngineByName(string name)
        {
            return _searchEngines.FirstOrDefault(engine => 
                engine.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public IEnumerable<ISearchEngineService> GetAllSearchEngines()
        {
            return _searchEngines.AsReadOnly();
        }

        /// <summary>
        /// Gets the preferred search engine name from user settings
        /// </summary>
        private string GetPreferredSearchEngineName()
        {
            var preferredEngine = _userSettingsService.GeneralSettingsService.PreferredSearchEngine;
            return preferredEngine switch
            {
                PreferredSearchEngine.Everything => "Everything",
                PreferredSearchEngine.Windows => "Windows Search",
                _ => "Windows Search"
            };
        }
    }
}
