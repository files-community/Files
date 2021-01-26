using Files.Enums;
using Files.Helpers;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.Cloud.Providers
{
    public class MegaCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                var infoPath = @"Mega Limited\MEGAsync\MEGAsync.cfg";
                var configPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(configPath);
                var parser = new IniDataParser();
                var data = parser.Parse(await FileIO.ReadTextAsync(configFile));
                byte[] fixedSeed = Encoding.UTF8.GetBytes("$JY/X?o=h·&%v/M(");
                byte[] localKey = GetLocalStorageKey();
                byte[] xLocalKey = XOR(fixedSeed, localKey);
                var sh = SHA1.Create();
                byte[] encryptionKey = sh.ComputeHash(xLocalKey);

                var mainSection = data.Sections.First(s => s.SectionName == "General");
                string currentGroup = "";

                var currentAccountKey = Hash("currentAccount", currentGroup, encryptionKey);
                var currentAccountStr = mainSection.Keys.First(s => s.KeyName == currentAccountKey);
                var currentAccountDecrypted = Decrypt(currentAccountKey, currentAccountStr.Value.Replace("\"", ""), currentGroup);

                var currentAccountSectionKey = Hash(currentAccountDecrypted, "", encryptionKey);
                var currentAccountSection = data.Sections.First(s => s.SectionName == currentAccountSectionKey);

                var syncKey = Hash("Syncs", currentAccountSectionKey, encryptionKey);
                var syncGroups = currentAccountSection.Keys.Where(s => s.KeyName.StartsWith(syncKey)).Select(x => x.KeyName.Split('\\')[1]).Distinct();
                var results = new List<CloudProvider>();

                foreach (var sync in syncGroups)
                {
                    currentGroup = string.Join("/", currentAccountSectionKey, syncKey, sync);
                    var syncNameKey = Hash("syncName", currentGroup, encryptionKey);
                    var syncNameStr = currentAccountSection.Keys.First(s => s.KeyName == string.Join("\\", syncKey, sync, syncNameKey));
                    var syncNameDecrypted = Decrypt(syncNameKey, syncNameStr.Value.Replace("\"", ""), currentGroup);
                    var localFolderKey = Hash("localFolder", currentGroup, encryptionKey);
                    var localFolderStr = currentAccountSection.Keys.First(s => s.KeyName == string.Join("\\", syncKey, sync, localFolderKey));
                    var localFolderDecrypted = Decrypt(localFolderKey, localFolderStr.Value.Replace("\"", ""), currentGroup);

                    results.Add(new CloudProvider()
                    {
                        ID = CloudProviders.Mega,
                        Name = $"MEGA ({syncNameDecrypted})",
                        SyncFolder = localFolderDecrypted
                    });
                }

                return results;
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }

        private byte[] GetLocalStorageKey()
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

            int dwLength = NativeWinApiHelper.GetLengthSid(userStruct.User.Sid);
            byte[] result = new byte[dwLength];
            Marshal.Copy(userStruct.User.Sid, result, 0, dwLength);
            NativeWinApiHelper.CloseHandle(hToken);
            Marshal.FreeHGlobal(userToken);
            return result;
        }

        private byte[] XOR(byte[] key, byte[] data)
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

        private string Hash(string key, string group, byte[] encryptionKey)
        {
            var sh = SHA1.Create();
            byte[] xPath = XOR(encryptionKey, Encoding.UTF8.GetBytes(key + group));
            byte[] keyHash = sh.ComputeHash(xPath);
            byte[] xKeyHash = XOR(Encoding.UTF8.GetBytes(key), keyHash);
            return ByteArrayToString(xKeyHash);
        }

        private string Decrypt(string key, string value, string group)
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

        private string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "").ToLowerInvariant();
        }
    }
}