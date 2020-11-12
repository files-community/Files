using Files.Helpers;
using IniParser.Parser;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem
{
    public enum KnownCloudProviders
    {
        OneDrive,
        OneDriveBusiness,
        Mega,
        GoogleDrive,
        DropBox,
        iCloud,
        Box
    }

    public class CloudProvider
    {
        public KnownCloudProviders ID { get; set; }
        public string SyncFolder { get; set; }
        public string Name { get; set; }

        public static async Task<List<CloudProvider>> GetInstalledCloudProviders()
        {
            var providerList = new List<CloudProvider>();
            DetectOneDrive(providerList);
            await DetectGoogleDriveAsync(providerList);
            await DetectDropboxAsync(providerList);
            await DetectMegaAsync(providerList);
            await DetectBoxAsync(providerList);
            await DetectiCloudAsync(providerList);
            return providerList;
        }

        #region Box
        private static async Task DetectBoxAsync(List<CloudProvider> providerList)
        {
            try
            {
                var infoPath = @"Box\Box\data\shell\sync_root_folder.txt";
                var configPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(configPath);
                var syncPath = await FileIO.ReadTextAsync(configFile);
                if (!string.IsNullOrEmpty(syncPath))
                {
                    providerList.Add(new CloudProvider()
                    {
                        ID = KnownCloudProviders.Box,
                        SyncFolder = syncPath,
                        Name = "Box"
                    });
                }
            }
            catch
            {
                // Not detected
            }
        }
        #endregion

        #region iCloud
        private static async Task DetectiCloudAsync(List<CloudProvider> providerList)
        {
            try
            {
                var userPath = UserDataPaths.GetDefault().Profile;
                var iCloudPath = "iCloudDrive";
                var driveFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(userPath, iCloudPath));
                providerList.Add(new CloudProvider()
                {
                    ID = KnownCloudProviders.iCloud,
                    SyncFolder = driveFolder.Path,
                    Name = "iCloud"
                });
            }
            catch
            {
                // Not detected
            }
        }
        #endregion

        #region DropBox
        private static async Task DetectDropboxAsync(List<CloudProvider> providerList)
        {
            try
            {
                var infoPath = @"Dropbox\info.json";
                var jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
                var jsonObj = JObject.Parse(await FileIO.ReadTextAsync(configFile));
                var dropboxPath = (string)(jsonObj["personal"]["path"]);
                providerList.Add(new CloudProvider()
                {
                    ID = KnownCloudProviders.DropBox,
                    SyncFolder = dropboxPath,
                    Name = "Dropbox"
                });
            }
            catch
            {
                // Not detected
            }
        }
        #endregion

        #region Mega
        private static async Task DetectMegaAsync(List<CloudProvider> providerList)
        {
            try
            {
                //var sidstring = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
                //using var sid = AdvApi32.ConvertStringSidToSid(sidstring);
                var infoPath = @"Mega Limited\MEGAsync\MEGAsync.cfg";
                var configPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(configPath);
                var parser = new IniDataParser();
                var data = parser.Parse(await FileIO.ReadTextAsync(configFile));
                byte[] fixedSeed = Encoding.UTF8.GetBytes("$JY/X?o=h·&%v/M(");
                byte[] localKey = GetLocalStorageKey(); /*sid.GetBinaryForm()*/
                byte[] xLocalKey = XOR(fixedSeed, localKey);
                var sh = SHA1.Create();
                byte[] hLocalKey = sh.ComputeHash(xLocalKey);
                var encryptionKey = hLocalKey;

                var mainSection = data.Sections.First(s => s.SectionName == "General");
                string currentGroup = "";

                var currentAccountKey = Hash("currentAccount", currentGroup, encryptionKey);
                var currentAccountStr = mainSection.Keys.First(s => s.KeyName == currentAccountKey);
                var currentAccountDecrypted = Decrypt(currentAccountKey, currentAccountStr.Value.Replace("\"", ""), currentGroup);

                var currentAccountSectionKey = Hash(currentAccountDecrypted, "", encryptionKey);
                var currentAccountSection = data.Sections.First(s => s.SectionName == currentAccountSectionKey);

                var syncKey = Hash("Syncs", currentAccountSectionKey, encryptionKey);
                var syncGroups = currentAccountSection.Keys.Where(s => s.KeyName.StartsWith(syncKey)).Select(x => x.KeyName.Split('\\')[1]).Distinct();
                foreach (var sync in syncGroups)
                {
                    currentGroup = string.Join("/", currentAccountSectionKey, syncKey, sync);
                    var syncNameKey = Hash("syncName", currentGroup, encryptionKey);
                    var syncNameStr = currentAccountSection.Keys.First(s => s.KeyName == string.Join("\\", syncKey, sync, syncNameKey));
                    var syncNameDecrypted = Decrypt(syncNameKey, syncNameStr.Value.Replace("\"", ""), currentGroup);
                    var localFolderKey = Hash("localFolder", currentGroup, encryptionKey);
                    var localFolderStr = currentAccountSection.Keys.First(s => s.KeyName == string.Join("\\", syncKey, sync, localFolderKey));
                    var localFolderDecrypted = Decrypt(localFolderKey, localFolderStr.Value.Replace("\"", ""), currentGroup);
                    providerList.Add(new CloudProvider()
                    {
                        ID = KnownCloudProviders.Mega,
                        SyncFolder = localFolderDecrypted,
                        Name = $"MEGA ({syncNameDecrypted})"
                    });
                }
            }
            catch
            {
                // Not detected
            }
        }

        private static byte[] GetLocalStorageKey()
        {
            if (!NativeWinApiHelper.OpenProcessToken(NativeWinApiHelper.GetCurrentProcess(), NativeWinApiHelper.TokenAccess.TOKEN_QUERY, out var hToken))
            {
                return null;
            }

            NativeWinApiHelper.GetTokenInformation(hToken, NativeWinApiHelper.TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out int dwBufferSize);
            if (dwBufferSize == 0)
            {
                NativeWinApiHelper.CloseHandle(hToken);
                return null;
            }

            IntPtr userToken = Marshal.AllocHGlobal(dwBufferSize);
            if (!NativeWinApiHelper.GetTokenInformation(hToken, NativeWinApiHelper.TOKEN_INFORMATION_CLASS.TokenUser, userToken, dwBufferSize, out var dwInfoBufferSize))
            {
                NativeWinApiHelper.CloseHandle(hToken);
                Marshal.FreeHGlobal(userToken);
                return null;
            }

            var userStruct = (NativeWinApiHelper.TOKEN_USER)Marshal.PtrToStructure(userToken, typeof(NativeWinApiHelper.TOKEN_USER));
            /*if (!userStruct.User.Sid.IsValidSid())
            {
                NativeWinApiHelper.CloseHandle(hToken);
                Marshal.FreeHGlobal(userToken);
                return null;
            }*/

            int dwLength = NativeWinApiHelper.GetLengthSid(userStruct.User.Sid);
            byte[] result = new byte[dwLength];
            Marshal.Copy(userStruct.User.Sid, result, 0, dwLength);
            NativeWinApiHelper.CloseHandle(hToken);
            Marshal.FreeHGlobal(userToken);
            return result;
        }

        private static string Decrypt(string key, string value, string group)
        {
            byte[] k = Encoding.ASCII.GetBytes(key);
            byte[] xValue = XOR(k, Convert.FromBase64String(value));
            byte[] xKey = XOR(k, Encoding.ASCII.GetBytes(group));

            IntPtr xValuePtr = Marshal.AllocHGlobal(xValue.Length);
            Marshal.Copy(xValue, 0, xValuePtr, xValue.Length);
            IntPtr xKeyPtr = Marshal.AllocHGlobal(xKey.Length);
            Marshal.Copy(xKey, 0, xKeyPtr, xKey.Length);

            NativeWinApiHelper.CRYPTOAPI_BLOB dataIn = new NativeWinApiHelper.CRYPTOAPI_BLOB();
            NativeWinApiHelper.CRYPTOAPI_BLOB entropy = new NativeWinApiHelper.CRYPTOAPI_BLOB();
            dataIn.pbData = xValuePtr;
            dataIn.cbData = (uint)xValue.Length;
            entropy.pbData = xKeyPtr;
            entropy.cbData = (uint)xKey.Length;

            if (!NativeWinApiHelper.CryptUnprotectData(dataIn, null, entropy, IntPtr.Zero, IntPtr.Zero, 0, out var dataOut))
            {
                Marshal.FreeHGlobal(xValuePtr);
                Marshal.FreeHGlobal(xKeyPtr);
                return null;
            }

            byte[] managedArray = new byte[dataOut.cbData];
            Marshal.Copy(dataOut.pbData, managedArray, 0, (int)dataOut.cbData);
            byte[] xDecrypted = XOR(k, managedArray);
            Marshal.FreeHGlobal(dataOut.pbData);
            Marshal.FreeHGlobal(xValuePtr);
            Marshal.FreeHGlobal(xKeyPtr);
            return Encoding.UTF8.GetString(xDecrypted);
        }

        private static string Hash(string key, string group, byte[] encryptionKey)
        {
            var sh = SHA1.Create();
            byte[] xPath = XOR(encryptionKey, Encoding.UTF8.GetBytes(key + group));
            byte[] keyHash = sh.ComputeHash(xPath);
            byte[] xKeyHash = XOR(Encoding.UTF8.GetBytes(key), keyHash);
            return ByteArrayToString(xKeyHash);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "").ToLowerInvariant();
        }

        private static byte[] XOR(byte[] key, byte[] data)
        {
            int keyLen = key.Length;
            if (keyLen == 0)
            {
                return data;
            }

            var result = new List<byte>();
            var k3 = key[keyLen / 3] >= 128 ? key[keyLen / 3] - 256 : key[keyLen / 3];
            var k5 = key[keyLen / 5] >= 128 ? key[keyLen / 5] - 256 : key[keyLen / 5];
            var k2 = key[keyLen / 2] >= 128 ? key[keyLen / 2] - 256 : key[keyLen / 2];
            var k7 = key[keyLen / 7] >= 128 ? key[keyLen / 7] - 256 : key[keyLen / 7];
            int rotation = Math.Abs(k3 * k5) % keyLen;
            int increment = Math.Abs(k2 * k7) % keyLen;
            for (int i = 0, j = rotation; i < data.Length; i++, j -= increment)
            {
                if (j < 0)
                {
                    j += keyLen;
                }
                result.Add((byte)(data[i] ^ key[j]));
            }
            return result.ToArray();
        }
        #endregion

        #region OneDrive
        private static void DetectOneDrive(List<CloudProvider> providerList)
        {
            try
            {
                var onedrive_personal = Environment.GetEnvironmentVariable("OneDriveConsumer");
                if (!string.IsNullOrEmpty(onedrive_personal))
                {
                    providerList.Add(new CloudProvider()
                    {
                        ID = KnownCloudProviders.OneDrive,
                        SyncFolder = onedrive_personal,
                        Name = $"OneDrive"
                    });
                }
                var onedrive_commercial = Environment.GetEnvironmentVariable("OneDriveCommercial");
                if (!string.IsNullOrEmpty(onedrive_commercial))
                {
                    providerList.Add(new CloudProvider()
                    {
                        ID = KnownCloudProviders.OneDriveBusiness,
                        SyncFolder = onedrive_commercial,
                        Name = $"OneDrive Commercial"
                    });
                }
            }
            catch
            {
                // Not detected
            }
        }
        #endregion

        #region GoogleDrive
        private static async Task DetectGoogleDriveAsync(List<CloudProvider> providerList)
        {
            try
            {
                // Google Drive's sync database can be in a couple different locations. Go find it. 
                string appDataPath = UserDataPaths.GetDefault().LocalAppData;
                string dbPath = @"Google\Drive\user_default\sync_config.db";
                var configFile = await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, dbPath));
                await configFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db", NameCollisionOption.ReplaceExisting);
                var syncDbPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "google_drive.db");

                // Build the connection and sql command
                SQLitePCL.Batteries_V2.Init();
                using (var con = new SqliteConnection($"Data Source='{syncDbPath}'"))
                using (var cmd = new SqliteCommand("select * from data where entry_key='local_sync_root_path'", con))
                {
                    // Open the connection and execute the command
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    reader.Read();

                    // Extract the data from the reader
                    string path = reader["data_value"]?.ToString();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return;
                    }

                    // By default, the path will be prefixed with "\\?\" (unless another app has explicitly changed it).
                    // \\?\ indicates to Win32 that the filename may be longer than MAX_PATH (see MSDN).
                    // Parts of .NET (e.g. the File class) don't handle this very well, so remove this prefix.
                    if (path.StartsWith(@"\\?\"))
                    {
                        path = path.Substring(@"\\?\".Length);
                    }

                    providerList.Add(new CloudProvider()
                    {
                        ID = KnownCloudProviders.GoogleDrive,
                        SyncFolder = path,
                        Name = $"Google Drive"
                    });
                }
            }
            catch
            {
                // Not detected
            }
        }
        #endregion
    }
}
