using Files.Common;
using Files.Extensions;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem.Permissions
{
    public class UserGroup : ObservableObject
    {
        public string Glyph => ItemType switch
        {
            SecurityType.User => "\xE2AF",
            SecurityType.Group => "\xE902",
            _ => "\xE716",
        };

        [JsonIgnore]
        public byte[] IconData { get; set; }

        private BitmapImage icon;
        [JsonIgnore]
        public BitmapImage Icon
        {
            get => icon;
            set
            {
                if (SetProperty(ref icon, value))
                {
                    OnPropertyChanged(nameof(LoadIcon));
                }
            }
        }
        [JsonIgnore]
        public bool LoadIcon => Icon != null;

        public string Sid { get; set; }
        public string Name { get; set; }
        public string Domain { get; set; }
        public string FullName => string.IsNullOrEmpty(Domain) ? Name : $"{Domain}\\{Name}";
        public List<string> Groups { get; set; }
        public SecurityType ItemType { get; set; }

        public UserGroup()
        {
            Groups = new List<string>();
        }

        public static UserGroup FromSid(string sid)
        {
            var userGroup = new UserGroup() { Sid = sid };
            userGroup.GetUserGroupInfo();
            return userGroup;
        }

        public async Task LoadUserGroupTile()
        {
            if (string.IsNullOrEmpty(Domain))
            {
                return;
            }

            if (Domain == Environment.UserDomainName)
            {
                var localAccounts = await User.FindAllAsync(UserType.LocalUser | UserType.LocalGuest);
                var matches = await localAccounts.WhereAsync(async (x) => await x.GetPropertyAsync(KnownUserProperties.DisplayName) as string == Name);
                var streamRef = await matches.FirstOrDefault()?.GetPictureAsync(UserPictureSize.Size208x208);
                if (streamRef != null)
                {
                    using var stream = await streamRef.OpenReadAsync();
                    IconData = await stream.ToByteArrayAsync();
                }
            }
        }

        public void GetUserGroupInfo()
        {
            if (string.IsNullOrEmpty(Sid))
            {
                return;
            }

            ConvertStringSidToSid(Sid, out IntPtr sidPtr);
            int size = GetLengthSid(sidPtr);
            var binarySID = new byte[size];
            Marshal.Copy(sidPtr, binarySID, 0, size);
            Marshal.FreeHGlobal(sidPtr);

            var userName = new StringBuilder();
            var domainName = new StringBuilder();
            int cchUserName = 0, cchDomainName = 0;
            LookupAccountSid(null, binarySID, userName, ref cchUserName, domainName, ref cchDomainName, out _);
            userName.EnsureCapacity(cchUserName);
            domainName.EnsureCapacity(cchDomainName);

            if (LookupAccountSid(null, binarySID, userName, ref cchUserName, domainName, ref cchDomainName, out var secType))
            {
                ItemType = secType switch
                {
                    var x when
                        x == SID_NAME_USE.SidTypeAlias ||
                        x == SID_NAME_USE.SidTypeGroup ||
                        x == SID_NAME_USE.SidTypeWellKnownGroup => SecurityType.Group,

                    SID_NAME_USE.SidTypeUser => SecurityType.User,

                    _ => SecurityType.Other
                };
                Name = userName.ToString();
                Domain = domainName.ToString();
            }
        }

        private enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        [DllImport("api-ms-win-security-lsalookup-l2-1-0.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LookupAccountSid(
            string lpSystemName,
            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
            StringBuilder lpName,
            ref int cchName,
            StringBuilder ReferencedDomainName,
            ref int cchReferencedDomainName,
            out SID_NAME_USE peUse);

        [DllImport("api-ms-win-security-sddl-l1-1-0.dll", SetLastError = true)]
        private static extern bool ConvertStringSidToSid(
            string StringSid,
            out IntPtr ptrSid);

        [DllImport("api-ms-win-security-base-l1-1-0.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int GetLengthSid(IntPtr pSid);

        public WellKnownUserGroup GetWellKnownUserGroupType()
        {
            return Sid switch
            {
                "S-1-1-0" => WellKnownUserGroup.Everyone,
                "S-1-5-32-544" => WellKnownUserGroup.AdminGroup,
                "S-1-5-21domain-500" => WellKnownUserGroup.Administrator,
                "S-1-5-18" => WellKnownUserGroup.LocalSystem,
                _ => WellKnownUserGroup.None
            };
        }
    }

    public enum SecurityType
    {
        User,
        Group,
        Other
    };

    public enum WellKnownUserGroup
    {
        None,
        Everyone,
        AdminGroup,
        Administrator,
        LocalSystem,
    }
}
