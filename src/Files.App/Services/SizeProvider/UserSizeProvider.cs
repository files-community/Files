// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.SizeProvider;
using Files.App.Services.Search;

namespace Files.App.Services
{
	public sealed partial class UserSizeProvider : ISizeProvider
	{
		private readonly IFoldersSettingsService folderPreferences
			= Ioc.Default.GetRequiredService<IFoldersSettingsService>();
		private readonly IGeneralSettingsService generalSettings
			= Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private readonly IEverythingSearchService everythingSearchService
			= Ioc.Default.GetRequiredService<IEverythingSearchService>();

		private ISizeProvider provider;

		public event EventHandler<SizeChangedEventArgs> SizeChanged;

		public UserSizeProvider()
		{
			provider = GetProvider();
			provider.SizeChanged += Provider_SizeChanged;

			folderPreferences.PropertyChanged += FolderPreferences_PropertyChanged;
			generalSettings.PropertyChanged += GeneralSettings_PropertyChanged;
		}

		public Task CleanAsync()
			=> provider.CleanAsync();

		public async Task ClearAsync()
			=> await provider.ClearAsync();

		public Task UpdateAsync(string path, CancellationToken cancellationToken)
			=> provider.UpdateAsync(path, cancellationToken);

		public bool TryGetSize(string path, out ulong size)
			=> provider.TryGetSize(path, out size);

		public void Dispose()
		{
			provider.Dispose();
			folderPreferences.PropertyChanged -= FolderPreferences_PropertyChanged;
			generalSettings.PropertyChanged -= GeneralSettings_PropertyChanged;
		}

		private ISizeProvider GetProvider()
		{
			if (!folderPreferences.CalculateFolderSizes)
				return new NoSizeProvider();

			// Use Everything for folder sizes if it's selected and available
			if (generalSettings.PreferredSearchEngine == Data.Enums.PreferredSearchEngine.Everything)
			{
				if (everythingSearchService.IsEverythingAvailable())
				{
					return new EverythingSizeProvider(everythingSearchService, generalSettings);
				}
				else
				{
				}
			}

			// Fall back to standard provider
			return new DrivesSizeProvider();
		}

		private async void FolderPreferences_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.CalculateFolderSizes))
			{
				await provider.ClearAsync();
				provider.SizeChanged -= Provider_SizeChanged;
				provider = GetProvider();
				provider.SizeChanged += Provider_SizeChanged;
			}
		}

		private async void GeneralSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IGeneralSettingsService.PreferredSearchEngine) ||
				e.PropertyName is nameof(IGeneralSettingsService.EverythingMaxFolderSizeResults))
			{
				// Only update if folder size calculation is enabled
				if (folderPreferences.CalculateFolderSizes)
				{
					await provider.ClearAsync();
					provider.SizeChanged -= Provider_SizeChanged;
					provider = GetProvider();
					provider.SizeChanged += Provider_SizeChanged;
				}
			}
		}

		private void Provider_SizeChanged(object sender, SizeChangedEventArgs e)
			=> SizeChanged?.Invoke(this, e);
	}
}
