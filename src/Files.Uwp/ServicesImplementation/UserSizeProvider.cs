using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Backend.Services.SizeProvider;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Uwp.ServicesImplementation
{
    public class UserSizeProvider : ISizeProvider
    {
        private readonly IPreferencesSettingsService preferences
            = Ioc.Default.GetRequiredService<IPreferencesSettingsService>();

        private ISizeProvider provider;

        public event EventHandler<SizeChangedEventArgs> SizeChanged;

        public UserSizeProvider()
        {
            provider = GetProvider();
            provider.SizeChanged += Provider_SizeChanged;

            preferences.PropertyChanged += Preferences_PropertyChanged;
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
            preferences.PropertyChanged -= Preferences_PropertyChanged;
        }

        private ISizeProvider GetProvider()
            => preferences.ShowFolderSize ? new DrivesSizeProvider() : new NoSizeProvider();

        private async void Preferences_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IPreferencesSettingsService.ShowFolderSize))
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
