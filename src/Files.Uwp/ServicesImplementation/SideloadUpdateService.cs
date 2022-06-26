using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Shared;

namespace Files.Uwp.ServicesImplementation
{
    public class SideloadUpdateService : ObservableObject, IUpdateService
    {
        private const string SideloadStable = "https://cdn.files.community/files/stable/Files.Package.appinstaller";
        private const string SideloadPreview = "https://cdn.files.community/files/preview/Files.Package.appinstaller";

        private bool _isUpdateAvailable;
        private bool _isUpdating;
        private int _downloadPercentage;

        private readonly Dictionary<string, string> _sideloadVersion = new()
        {
            { "Files", SideloadStable },
            { "FilesPreview", SideloadPreview }
        };

        private const string TemporaryUpdatePackageName = "UpdatePackage.msix";

        private ILogger Logger { get; } = Ioc.Default.GetService<ILogger>();

        private string PackageName { get; } = Package.Current.Id.Name;

        private Version PackageVersion { get; } = new(Package.Current.Id.Version.Major,
            Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

        private Uri DownloadUri { get; set; }

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

        public int DownloadPercentage
        {
            get => _downloadPercentage;
            private set => SetProperty(ref _downloadPercentage, value);
        }

        public async Task DownloadUpdates()
        {
            await ApplyPackageUpdate();
        }

        public Task DownloadMandatoryUpdates()
        {
            return Task.CompletedTask;
        }

        public async Task CheckForUpdates()
        {
            Logger.Info($"SIDELOAD: Checking for updates...");
            await CheckForRemoteUpdate(_sideloadVersion[PackageName]);
        }

        private async Task CheckForRemoteUpdate(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException(nameof(uri));
            }

            try
            {
                using var client = new WebClient();
                using var stream = await client.OpenReadTaskAsync(uri);

                // Deserialize AppInstaller.
                XmlSerializer xml = new XmlSerializer(typeof(AppInstaller));
                var appInstaller = (AppInstaller)xml.Deserialize(stream);

                if (appInstaller == null)
                {
                    throw new ArgumentNullException(nameof(appInstaller));
                }

                var remoteVersion = new Version(appInstaller.Version);

                Logger.Info($"SIDELOAD: Current Package Name: {PackageName}");
                Logger.Info($"SIDELOAD: Remote Package Name: {appInstaller.MainBundle.Name}");
                Logger.Info($"SIDELOAD: Current Version: {PackageVersion}");
                Logger.Info($"SIDELOAD: Remote Version: {remoteVersion}");

                // Check details and version number.
                if (appInstaller.MainBundle.Name.Equals(PackageName) && remoteVersion.CompareTo(PackageVersion) > 0)
                {
                    Logger.Info("SIDELOAD: Update found.");
                    Logger.Info("SIDELOAD: Starting background download.");
                    DownloadUri = new Uri(appInstaller.MainBundle.Uri);
                    await StartBackgroundDownload();
                }
                else
                {
                    Logger.Warn("SIDELOAD: Update not found.");
                    IsUpdateAvailable = false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
        }

        private async Task StartBackgroundDownload()
        {
            try
            {
                using var client = new WebClient();
                client.DownloadFileCompleted += BackgroundDownloadCompleted;
                client.DownloadProgressChanged += BackgroundDownloadProgressChanged;

                // Use temp folder instead?
                var tempDownloadPath = ApplicationData.Current.LocalFolder.Path + "\\" + TemporaryUpdatePackageName;

                Stopwatch timer = Stopwatch.StartNew();

                await client.DownloadFileTaskAsync(DownloadUri, tempDownloadPath);

                timer.Stop();
                var timespan = timer.Elapsed;

                Logger.Info($"Download time taken: {timespan.Hours:00}:{timespan.Minutes:00}:{timespan.Seconds:00}");

                client.DownloadFileCompleted -= BackgroundDownloadCompleted;
                client.DownloadProgressChanged -= BackgroundDownloadProgressChanged;
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
        }

        private void BackgroundDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadPercentage = e.ProgressPercentage;
        }

        private void BackgroundDownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            IsUpdateAvailable = true;
        }

        private async Task ApplyPackageUpdate()
        {
            if (!IsUpdateAvailable)
            {
                return;
            }

            IsUpdating = true;

            PackageManager pm = new PackageManager();
            DeploymentResult result = null;

            try
            {
                await Task.Run(async () =>
                {
                    var bundlePath = new Uri(ApplicationData.Current.LocalFolder.Path + "\\" + TemporaryUpdatePackageName);

                    var deployment = pm.RequestAddPackageAsync(
                        bundlePath,
                        null,
                        DeploymentOptions.ForceTargetApplicationShutdown,
                        pm.GetDefaultPackageVolume(),
                        null,
                        null);

                    result = await deployment;
                });
            }
            catch (Exception e)
            {
                if (result?.ExtendedErrorCode != null)
                {
                    Logger.Info(result.ErrorText);
                }

                Logger.Error(e, e.Message);
            }
            finally
            {
                // Reset fields.
                IsUpdating = false;
                IsUpdateAvailable = false;
                DownloadPercentage = 0;
                DownloadUri = null;
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
