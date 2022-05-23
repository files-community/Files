using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Services;

namespace Files.Uwp.ServicesImplementation
{
    public class SideloadUpdateService : ObservableObject, IUpdateService
    {
        private const string SideloadStable = "https://cdn.files.community/files/stable/Files.Package.appinstaller";
        private const string SideloadPreview = "https://cdn.files.community/files/preview/Files.Package.appinstaller";

        private readonly Dictionary<string, string> _sideloadVersion = new()
        {
            { "Files", SideloadStable },
            { "FilesPreview", SideloadPreview }
        };

        private Uri DownloadUri { get; set; }

        private bool _isUpdateAvailable;
        private bool _isUpdating;

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            private set => SetProperty(ref _isUpdateAvailable, value);
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            private set => SetProperty(ref _isUpdating, value);
        }

        public async Task DownloadUpdates()
        {
            if (!IsUpdateAvailable)
            {
                return;
            }

            IsUpdating = true;

            PackageManager pm = new PackageManager();

            // Use DeploymentOptions.ForceApplicationShutdown to force shutdown.
            await pm.UpdatePackageAsync(DownloadUri, null, DeploymentOptions.None);

            IsUpdating = false;
            IsUpdateAvailable = false;
        }

        public Task DownloadMandatoryUpdates()
        {
            throw new NotImplementedException();
        }

        public async Task CheckForUpdates()
        {
            await CheckForRemoteUpdate(_sideloadVersion[Package.Current.Id.Name]);
        }

        private async Task CheckForRemoteUpdate(string uri)
        {
            using var client = new WebClient();
            using var stream = await client.OpenReadTaskAsync(uri);

            // Deserialize AppInstaller.
            XmlSerializer xml = new XmlSerializer(typeof(AppInstaller));
            var appInstaller = (AppInstaller)xml.Deserialize(stream);

            // Get version and package details.
            var currentPackageVersion = Package.Current.Id.Version;
            var currentPackageName = Package.Current.Id.Name;

            var currentVersion = new Version(currentPackageVersion.Major, currentPackageVersion.Minor,
                currentPackageVersion.Build, currentPackageVersion.Revision);
            var remoteVersion = new Version(appInstaller.Version);

            // Check details and version number.
            if (appInstaller.MainBundle.Name.Equals(currentPackageName) && remoteVersion.CompareTo(currentVersion) > 0)
            {
                DownloadUri = new Uri(appInstaller.MainBundle.Uri);
                IsUpdateAvailable = true;
            }
            else
            {
                IsUpdateAvailable = false;
            }
        }
    }

    /// <summary>
    /// AppInstaller class to hold information about remote updates.
    /// </summary>
    [XmlRoot(ElementName = "AppInstaller", Namespace = "http://schemas.microsoft.com/appx/appinstaller/2018")]
    public class AppInstaller
    {
        [XmlElement("MainBundle")]
        public MainBundle MainBundle { get; set; }

        [XmlAttribute("Uri")]
        public string Uri { get; set; }

        [XmlAttribute("Version")]
        public string Version { get; set; }
    }

    public class MainBundle
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Version")]
        public string Version { get; set; }

        [XmlAttribute("Uri")]
        public string Uri { get; set; }
    }
}
