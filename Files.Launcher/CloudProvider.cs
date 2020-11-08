using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace FilesFullTrust
{
    public enum KnownCloudProviders
    {
        ONEDRIVE,
        ONEDRIVE_BUSINESS,
        MEGASYNC,
        GOOGLEDRIVE,
        DROPBOX
    }

    public class CloudProvider
    {
        public KnownCloudProviders ID { get; set; }
        public string SyncFolder { get; set; }

        public static List<CloudProvider> GetInstalledCloudProviders()
        {
            var ret = new List<CloudProvider>();
            DetectOneDrive(ret);
            DetectGoogleDrive(ret);
            DetectDropbox(ret);
            DetectMegaSync(ret);
            return ret;
        }

        private static void DetectDropbox(List<CloudProvider> ret)
        {
            try
            {
                var infoPath = @"Dropbox\info.json";
                var jsonPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), infoPath);
                if (!File.Exists(jsonPath)) jsonPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), infoPath);
                if (!File.Exists(jsonPath)) return;
                var jsonObj = JObject.Parse(File.ReadAllText(jsonPath));
                var dropboxPath = (string)(jsonObj["personal"]["path"]);
                ret.Add(new CloudProvider() { ID = KnownCloudProviders.DROPBOX, SyncFolder = dropboxPath });
            }
            catch
            {
                // Not detected
            }
        }

        private static void DetectMegaSync(List<CloudProvider> ret)
        {
            try
            {
                string progName = @"MEGAsync\MEGAsync.exe";
                var progPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), progName);
                if (!File.Exists(progPath)) return;
                foreach (var si in ShellFolder.Desktop)
                {
                    try
                    {
                        if (!si.IsFileSystem) continue;
                        var shfi = new Shell32.SHFILEINFO();
                        var res = Shell32.SHGetFileInfo(si.FileSystemPath, 0, ref shfi, Shell32.SHFILEINFO.Size, Shell32.SHGFI.SHGFI_ICONLOCATION);
                        if (res == IntPtr.Zero) continue;
                        if (shfi.szDisplayName == progPath)
                        {
                            ret.Add(new CloudProvider() { ID = KnownCloudProviders.MEGASYNC, SyncFolder = si.ParsingName });
                        }
                    }
                    finally
                    {
                        si.Dispose();
                    }
                }
            }
            catch
            {
                // Not detected
            }
        }

        private static void DetectOneDrive(List<CloudProvider> ret)
        {
            try
            {
                var onedrive_personal = Environment.GetEnvironmentVariable("OneDrive");
                if (!string.IsNullOrEmpty(onedrive_personal))
                {
                    ret.Add(new CloudProvider() { ID = KnownCloudProviders.ONEDRIVE, SyncFolder = onedrive_personal });
                }
                var onedrive_commercial = Environment.GetEnvironmentVariable("OneDriveCommercial");
                if (!string.IsNullOrEmpty(onedrive_commercial))
                {
                    ret.Add(new CloudProvider() { ID = KnownCloudProviders.ONEDRIVE_BUSINESS, SyncFolder = onedrive_commercial });
                }
            }
            catch
            {
                // Not detected
            }
        }

        private static void DetectGoogleDrive(List<CloudProvider> ret)
        {
            try
            {
                // Google Drive's sync database can be in a couple different locations. Go find it. 
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dbName = "sync_config.db";
                var pathsToTry = new[] { @"Google\Drive\" + dbName, @"Google\Drive\user_default\" + dbName };

                string syncDbPath = (from p in pathsToTry
                                     where File.Exists(Path.Combine(appDataPath, p))
                                     select Path.Combine(appDataPath, p))
                                    .FirstOrDefault();
                if (syncDbPath == null) return;

                // Build the connection and sql command
                using (var con = new SqliteConnection($"Data Source='{syncDbPath}'"))
                using (var cmd = new SqliteCommand("select * from data where entry_key='local_sync_root_path'", con))
                {
                    // Open the connection and execute the command
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    reader.Read();

                    // Extract the data from the reader
                    string path = reader["data_value"]?.ToString();
                    if (string.IsNullOrWhiteSpace(path)) return;

                    // By default, the path will be prefixed with "\\?\" (unless another app has explicitly changed it).
                    // \\?\ indicates to Win32 that the filename may be longer than MAX_PATH (see MSDN).
                    // Parts of .NET (e.g. the File class) don't handle this very well, so remove this prefix.
                    if (path.StartsWith(@"\\?\"))
                        path = path.Substring(@"\\?\".Length);

                    ret.Add(new CloudProvider() { ID = KnownCloudProviders.GOOGLEDRIVE, SyncFolder = path });
                }
            }
            catch
            {
                // Not detected
            }
        }
    }
}
