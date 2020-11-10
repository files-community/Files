using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

        # region DROPBOX
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
        #endregion

        #region MEGASYNC
        private static void DetectMegaSync(List<CloudProvider> ret)
        {
            try
            {
                var sidstring = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
                using var sid = AdvApi32.ConvertStringSidToSid(sidstring);
                var infoPath = @"Mega Limited\MEGAsync\MEGAsync.cfg";
                var configPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), infoPath);
                if (!File.Exists(configPath)) configPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), infoPath);
                if (!File.Exists(configPath)) return;
                var parser = new IniParser.FileIniDataParser();
                var data = parser.ReadFile(configPath);
                byte[] fixedSeed = Encoding.UTF8.GetBytes("$JY/X?o=h·&%v/M(");
                byte[] localKey = sid.GetBinaryForm();
                byte[] xLocalKey = XOR(fixedSeed, localKey);
                var sh = SHA1.Create();
                byte[] hLocalKey = sh.ComputeHash(xLocalKey);
                var encryptionKey = hLocalKey;

                var mainSection = data.Sections.First(s => s.SectionName == "General");
                string currentGroup = "";

                var currentAccountKey = hash("currentAccount", currentGroup, encryptionKey);
                var currentAccountStr = mainSection.Keys.First(s => s.KeyName == currentAccountKey);
                var currentAccountDecrypted = decrypt(currentAccountKey, currentAccountStr.Value.Replace("\"", ""), currentGroup);

                var currentAccountSectionKey = hash(currentAccountDecrypted, "", encryptionKey);
                var currentAccountSection = data.Sections.First(s => s.SectionName == currentAccountSectionKey);

                var syncKey = hash("Syncs", currentAccountSectionKey, encryptionKey);
                var syncGroups = currentAccountSection.Keys.Where(s => s.KeyName.StartsWith(syncKey)).Select(x => x.KeyName.Split('\\')[1]).Distinct();
                foreach (var sync in syncGroups)
                {
                    currentGroup = string.Join("/", currentAccountSectionKey, syncKey, sync);
                    // var tmp = hash("0", string.Join("/", currentAccountSectionKey, syncKey), encryptionKey);
                    var syncNameKey = hash("syncName", currentGroup, encryptionKey);
                    var syncNameStr = currentAccountSection.Keys.First(s => s.KeyName == string.Join("\\", syncKey, sync, syncNameKey));
                    var syncNameDecrypted = decrypt(syncNameKey, syncNameStr.Value.Replace("\"", ""), currentGroup);
                    var localFolderKey = hash("localFolder", currentGroup, encryptionKey);
                    var localFolderStr = currentAccountSection.Keys.First(s => s.KeyName == string.Join("\\", syncKey, sync, localFolderKey));
                    var localFolderDecrypted = decrypt(localFolderKey, localFolderStr.Value.Replace("\"", ""), currentGroup);
                    ret.Add(new CloudProvider() { ID = KnownCloudProviders.MEGASYNC, SyncFolder = localFolderDecrypted });
                }
            }
            catch
            {
                // Not detected
            }
        }

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(
            in Crypt32.CRYPTOAPI_BLOB pDataIn,
            StringBuilder szDataDescr,
            in Crypt32.CRYPTOAPI_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            IntPtr pPromptStruct,
            Crypt32.CryptProtectFlags dwFlags,
            out Crypt32.CRYPTOAPI_BLOB pDataOut);

        private static string decrypt(string key, string value, string group)
        {
            byte[] k = Encoding.ASCII.GetBytes(key);
            byte[] xValue = XOR(k, Convert.FromBase64String(value));
            byte[] xKey = XOR(k, Encoding.ASCII.GetBytes(group));

            IntPtr xValuePtr = Marshal.AllocHGlobal(xValue.Length);
            Marshal.Copy(xValue, 0, xValuePtr, xValue.Length);
            IntPtr xKeyPtr = Marshal.AllocHGlobal(xKey.Length);
            Marshal.Copy(xKey, 0, xKeyPtr, xKey.Length);

            Crypt32.CRYPTOAPI_BLOB dataIn = new Crypt32.CRYPTOAPI_BLOB();
            Crypt32.CRYPTOAPI_BLOB entropy = new Crypt32.CRYPTOAPI_BLOB();
            dataIn.pbData = xValuePtr;
            dataIn.cbData = (uint)xValue.Length;
            entropy.pbData = xKeyPtr;
            entropy.cbData = (uint)xKey.Length;

            if (!CryptUnprotectData(dataIn, null, entropy, IntPtr.Zero, IntPtr.Zero, 0, out var dataOut))
            {
                Marshal.FreeHGlobal(xValuePtr);
                Marshal.FreeHGlobal(xKeyPtr);
                return null;
            }

            byte[] managedArray = new byte[dataOut.cbData];
            Marshal.Copy(dataOut.pbData, managedArray, 0, (int)dataOut.cbData);
            byte[] xDecrypted = XOR(k, managedArray);
            Kernel32.LocalFree(dataOut.pbData);
            Marshal.FreeHGlobal(xValuePtr);
            Marshal.FreeHGlobal(xKeyPtr);
            return Encoding.UTF8.GetString(xDecrypted);
        }

        private static string hash(string key, string group, byte[] encryptionKey)
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

        #region ONEDRIVE
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
        #endregion

        #region GOOGLEDRIVE
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
        #endregion
    }
}
