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

        public uint DownloadPercentage { get; private set; }

        public async Task DownloadUpdates()
        {
            if (!IsUpdateAvailable)
            {
                return;
            }

            IsUpdating = true;

            App.Logger.Info($"SIDELOAD: Updating: {DownloadUri.AbsoluteUri}");

            PackageManager pm = new PackageManager();
            DeploymentResult deploymentResult = null;

            try
            {
                var progress = new Progress<DeploymentProgress>(report =>
                {
                    DownloadPercentage = report.percentage;
                    // UNDONE: Removed as it floods the log files.
                    // App.Logger.Info($"SIDELOAD: Download State: {report.state}");
                    // App.Logger.Info($"SIDELOAD: Download Percentage: {report.percentage}%");

                    if (DownloadPercentage == 100)
                    {
                        App.Logger.Info($"SIDELOAD: Finished updating: {DownloadUri.AbsoluteUri}");
                    }
                });

                // Have to use ForceTargetAppShutdown flag as the appinstaller won't update while it's being used.
                deploymentResult = await pm.RequestAddPackageByAppInstallerFileAsync(
                    DownloadUri,
                    AddPackageByAppInstallerOptions.ForceTargetAppShutdown,
                    pm.GetDefaultPackageVolume()).AsTask(progress);

            }
            catch (Exception e)
            {
                App.Logger.Error(e, e.Message);
                App.Logger.Info(deploymentResult?.ErrorText ?? "No error message from deploymentResult.");
            }
            finally
            {
                IsUpdating = false;
                IsUpdateAvailable = false;
            }
        }

        public Task DownloadMandatoryUpdates()
        {
            throw new NotSupportedException("This method is not supported by this service.");
        }

        public async Task CheckForUpdates()
        {
            var sideloadVersion = _sideloadVersion[Package.Current.Id.Name];
            App.Logger.Info($"SIDELOAD: Checking for updates...{sideloadVersion}");

            await CheckForRemoteUpdate(sideloadVersion);
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

            App.Logger.Info($"SIDELOAD: Current Package Name: {currentPackageName}");
            App.Logger.Info($"SIDELOAD: Remote Package Name: {appInstaller.MainBundle.Name}");
            App.Logger.Info($"SIDELOAD: Current Version: {currentVersion}");
            App.Logger.Info($"SIDELOAD: Remote Version: {remoteVersion}");

            // Check details and version number.
            if (appInstaller.MainBundle.Name.Equals(currentPackageName) && remoteVersion.CompareTo(currentVersion) > 0)
            {
                App.Logger.Info("SIDELOAD: Update found.");
                DownloadUri = new Uri(appInstaller.MainBundle.Uri);
                IsUpdateAvailable = true;
            }
            else
            {
                App.Logger.Warn("SIDELOAD: Update not available.");
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
