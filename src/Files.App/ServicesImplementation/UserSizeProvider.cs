using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Core.Services.Settings;
using Files.Core.Services.SizeProvider;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	public class UserSizeProvider : ISizeProvider
	{
		private readonly IFoldersSettingsService folderPreferences
			= Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		private ISizeProvider provider;

		public event EventHandler<SizeChangedEventArgs> SizeChanged;

		public UserSizeProvider()
		{
			provider = GetProvider();
			provider.SizeChanged += Provider_SizeChanged;

			folderPreferences.PropertyChanged += folderPreferences_PropertyChanged;
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
			folderPreferences.PropertyChanged -= folderPreferences_PropertyChanged;
		}

		private ISizeProvider GetProvider()
			=> folderPreferences.CalculateFolderSizes ? new DrivesSizeProvider() : new NoSizeProvider();

		private async void folderPreferences_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.CalculateFolderSizes))
			{
				await provider.ClearAsync();
				provider.SizeChanged -= Provider_SizeChanged;
				provider = GetProvider();
				provider.SizeChanged += Provider_SizeChanged;
			}
		}

		private void Provider_SizeChanged(object sender, SizeChangedEventArgs e)
			=> SizeChanged?.Invoke(this, e);
	}
}
