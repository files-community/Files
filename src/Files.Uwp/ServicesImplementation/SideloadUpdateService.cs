using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Services;

namespace Files.Uwp.ServicesImplementation
{
    public class SideloadUpdateService : ObservableObject, IUpdateService
    {
        private const string StableUri = "https://cdn.files.community/files/stable/Files.Package.appinstaller";
        private const string SideloadUri = "https://cdn.files.community/files/preview/Files.Package.appinstaller";

        private bool _isUpdateAvailable;
        private bool _isUpdating;

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => SetProperty(ref _isUpdateAvailable, value);
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }

        public Task DownloadUpdates()
        {
            throw new NotImplementedException();
        }

        public Task DownloadMandatoryUpdates()
        {
            throw new NotImplementedException();
        }

        public async Task CheckForUpdates()
        {
            if (!IsConnectedToInternet())
            {
                return;
            }

            await CheckForRemoteUpdate(SideloadUri);
        }

        private async Task CheckForRemoteUpdate(string uri)
        {
            using var client = new WebClient();
            using var stream = await client.OpenReadTaskAsync(uri);

            // Deserialize AppInstaller.
            XmlSerializer xml = new XmlSerializer(typeof(AppInstaller));
            var appInstaller = (AppInstaller)xml.Deserialize(stream);

            // Get version and app details.
            var packageVersion = Package.Current.Id.Version;
            var packageName = Package.Current.Id.Name;
            var version = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            var newVersion = new Version(appInstaller.Version);

            // Check details and version number.
            // We need to check that the names match. i.e. FilesPreview, Files
            if (appInstaller.MainBundle.Name.Equals(packageName) &&
                version.CompareTo(newVersion) > 0)
            {
                IsUpdateAvailable = true;
            }
            else
            {
                IsUpdateAvailable = false;
                App.Logger.Info("Package name didn't match, or version was greater.");
            }
        }

        private bool IsConnectedToInternet(int timeOut = 3000)
        {
            using var ping = new Ping();
            try
            {
                var reply = ping.Send(SideloadUri, timeOut);

                if (reply is not null & reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}
