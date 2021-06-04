using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem.Permissions
{
    public class FilePermissions
    {
        public string FilePath { get; set; }
        public bool IsFolder { get; set; }

        public bool CanReadFilePermissions { get; set; }

        public string OwnerSID { get; set; }

        [JsonIgnore]
        public UserGroup Owner => UserGroup.FromSid(OwnerSID);

        public string CurrentUserSID { get; set; }

        [JsonIgnore]
        public UserGroup CurrentUser => UserGroup.FromSid(CurrentUserSID);

        public FilePermissions()
        {
            AccessRules = new List<FileSystemAccessRule>();
        }

        public List<FileSystemAccessRule> AccessRules { get; set; }

        // Consolidated view 1
        [JsonIgnore]
        public List<RulesForUser> RulesForUsers => RulesForUser.ForAllUsers(this);

        public FileSystemRights GetEffectiveRights(UserGroup user)
        {
            var userSids = new List<string> { user.Sid };
            userSids.AddRange(user.Groups);

            FileSystemRights inheritedDenyRights = 0, denyRights = 0;
            FileSystemRights inheritedAllowRights = 0, allowRights = 0;

            foreach (FileSystemAccessRule Rule in AccessRules.Where(x => userSids.Contains(x.IdentityReference)))
            {
                if (Rule.AccessControlType == AccessControlType.Deny)
                {
                    if (Rule.IsInherited)
                    {
                        inheritedDenyRights |= Rule.FileSystemRights;
                    }
                    else
                    {
                        denyRights |= Rule.FileSystemRights;
                    }
                }
                else if (Rule.AccessControlType == AccessControlType.Allow)
                {
                    if (Rule.IsInherited)
                    {
                        inheritedAllowRights |= Rule.FileSystemRights;
                    }
                    else
                    {
                        allowRights |= Rule.FileSystemRights;
                    }
                }
            }

            return (inheritedAllowRights & ~inheritedDenyRights) | (allowRights & ~denyRights);
        }
    }

    public class RulesForUser : ObservableObject
    {
        private FilePermissions filePermissions;

        public RulesForUser(FilePermissions filePermissions)
        {
            this.filePermissions = filePermissions;
        }

        public FileSystemRights InheritedDenyRights { get; set; }
        public FileSystemRights InheritedAllowRights { get; set; }

        public FileSystemRights denyRights;
        public FileSystemRights DenyRights
        {
            get => denyRights;
            set
            {
                if (SetProperty(ref denyRights, value))
                {
                    OnPropertyChanged(nameof(DeniesWrite));
                    OnPropertyChanged(nameof(DeniesFullControl));
                    OnPropertyChanged(nameof(DeniesListDirectory));
                    OnPropertyChanged(nameof(DeniesModify));
                    OnPropertyChanged(nameof(DeniesRead));
                    OnPropertyChanged(nameof(DeniesReadAndExecute));
                }
            }
        }

        public FileSystemRights allowRights;
        public FileSystemRights AllowRights
        {
            get => allowRights;
            set
            {
                if (SetProperty(ref allowRights, value))
                {
                    OnPropertyChanged(nameof(GrantsWrite));
                    OnPropertyChanged(nameof(GrantsFullControl));
                    OnPropertyChanged(nameof(GrantsListDirectory));
                    OnPropertyChanged(nameof(GrantsModify));
                    OnPropertyChanged(nameof(GrantsRead));
                    OnPropertyChanged(nameof(GrantsReadAndExecute));
                }
            }
        }

        public UserGroup UserGroup { get; set; }

        public bool GrantsInheritedWrite => InheritedAllowRights.HasFlag(FileSystemRights.Write);
        public bool GrantsInheritedRead => InheritedAllowRights.HasFlag(FileSystemRights.Read);
        public bool GrantsInheritedListDirectory => InheritedAllowRights.HasFlag(FileSystemRights.ListDirectory);
        public bool GrantsInheritedReadAndExecute => InheritedAllowRights.HasFlag(FileSystemRights.ReadAndExecute);
        public bool GrantsInheritedModify => InheritedAllowRights.HasFlag(FileSystemRights.Modify);
        public bool GrantsInheritedFullControl => InheritedAllowRights.HasFlag(FileSystemRights.FullControl);

        public bool DeniesInheritedWrite => InheritedDenyRights.HasFlag(FileSystemRights.Write);
        public bool DeniesInheritedRead => InheritedDenyRights.HasFlag(FileSystemRights.Read);
        public bool DeniesInheritedListDirectory => InheritedDenyRights.HasFlag(FileSystemRights.ListDirectory);
        public bool DeniesInheritedReadAndExecute => InheritedDenyRights.HasFlag(FileSystemRights.ReadAndExecute);
        public bool DeniesInheritedModify => InheritedDenyRights.HasFlag(FileSystemRights.Modify);
        public bool DeniesInheritedFullControl => InheritedDenyRights.HasFlag(FileSystemRights.FullControl);

        private void UpdateAccessRules()
        {
            filePermissions.AccessRules.RemoveAll(x => x.IdentityReference == UserGroup.Sid && !x.IsInherited);
            if (AllowRights != 0 && !InheritedAllowRights.HasFlag(AllowRights)) // Do not set if permission is already granted by inheritance
            {
                filePermissions.AccessRules.Add(new FileSystemAccessRule()
                {
                    AccessControlType = AccessControlType.Allow,
                    FileSystemRights = AllowRights,
                    IdentityReference = UserGroup.Sid
                });
            }
            if (DenyRights != 0 && !InheritedDenyRights.HasFlag(DenyRights)) // Do not set if permission is already denied by inheritance
            {
                filePermissions.AccessRules.Add(new FileSystemAccessRule()
                {
                    AccessControlType = AccessControlType.Deny,
                    FileSystemRights = DenyRights,
                    IdentityReference = UserGroup.Sid
                });
            }
        }

        private void ToggleAllowPermission(FileSystemRights permission, bool value)
        {
            if (value && !AllowRights.HasFlag(permission) && !InheritedAllowRights.HasFlag(permission))
            {
                AllowRights |= permission;
                DenyRights = DenyRights & ~permission;
            }
            else if (!value && AllowRights.HasFlag(permission))
            {
                AllowRights = AllowRights & ~permission;
            }
            this.UpdateAccessRules();
        }

        private void ToggleDenyPermission(FileSystemRights permission, bool value)
        {
            if (value && !DenyRights.HasFlag(permission) && !InheritedDenyRights.HasFlag(permission))
            {
                DenyRights |= permission;
                AllowRights = AllowRights & ~permission;
            }
            else if (!value && DenyRights.HasFlag(permission))
            {
                DenyRights = DenyRights & ~permission;
            }
            this.UpdateAccessRules();
        }

        public bool GrantsWrite
        {
            get => AllowRights.HasFlag(FileSystemRights.Write) || GrantsInheritedWrite;
            set => ToggleAllowPermission(FileSystemRights.Write, value);
        }
        public bool GrantsRead
        {
            get => AllowRights.HasFlag(FileSystemRights.Read) || GrantsInheritedRead;
            set => ToggleAllowPermission(FileSystemRights.Read, value);
        }
        public bool GrantsListDirectory
        {
            get => AllowRights.HasFlag(FileSystemRights.ListDirectory) || GrantsInheritedListDirectory;
            set => ToggleAllowPermission(FileSystemRights.ListDirectory, value);
        }
        public bool GrantsReadAndExecute
        {
            get => AllowRights.HasFlag(FileSystemRights.ReadAndExecute) || GrantsInheritedReadAndExecute;
            set => ToggleAllowPermission(FileSystemRights.ReadAndExecute, value);
        }
        public bool GrantsModify
        {
            get => AllowRights.HasFlag(FileSystemRights.Modify) || GrantsInheritedModify;
            set => ToggleAllowPermission(FileSystemRights.Modify, value);
        }

        public bool GrantsFullControl
        {
            get => AllowRights.HasFlag(FileSystemRights.FullControl) || GrantsInheritedFullControl;
            set => ToggleAllowPermission(FileSystemRights.FullControl, value);
        }

        public bool DeniesWrite
        {
            get => DenyRights.HasFlag(FileSystemRights.Write) || DeniesInheritedWrite;
            set => ToggleDenyPermission(FileSystemRights.Write, value);
        }
        public bool DeniesRead
        {
            get => DenyRights.HasFlag(FileSystemRights.Read) || DeniesInheritedRead;
            set => ToggleDenyPermission(FileSystemRights.Read, value);
        }
        public bool DeniesListDirectory
        {
            get => DenyRights.HasFlag(FileSystemRights.ListDirectory) || DeniesInheritedListDirectory;
            set => ToggleDenyPermission(FileSystemRights.ListDirectory, value);
        }
        public bool DeniesReadAndExecute
        {
            get => DenyRights.HasFlag(FileSystemRights.ReadAndExecute) || DeniesInheritedReadAndExecute;
            set => ToggleDenyPermission(FileSystemRights.ReadAndExecute, value);
        }
        public bool DeniesModify
        {
            get => DenyRights.HasFlag(FileSystemRights.Modify) || DeniesInheritedModify;
            set => ToggleDenyPermission(FileSystemRights.Modify, value);
        }
        public bool DeniesFullControl
        {
            get => DenyRights.HasFlag(FileSystemRights.FullControl) || DeniesInheritedFullControl;
            set => ToggleDenyPermission(FileSystemRights.FullControl, value);
        }

        public static List<RulesForUser> ForAllUsers(FilePermissions filePermissions)
        {
            return filePermissions.AccessRules.Select(x => x.IdentityReference).Distinct().Select(x => RulesForUser.ForUser(filePermissions, x)).ToList();
        }

        public static RulesForUser ForUser(FilePermissions filePermissions, string identity)
        {
            var perm = new RulesForUser(filePermissions) { UserGroup = UserGroup.FromSid(identity) };
            foreach (var Rule in filePermissions.AccessRules.Where(x => x.IdentityReference == identity))
            {
                if (Rule.AccessControlType == AccessControlType.Deny)
                {
                    if (Rule.IsInherited)
                    {
                        perm.InheritedDenyRights |= Rule.FileSystemRights;
                    }
                    else
                    {
                        perm.DenyRights |= Rule.FileSystemRights;
                    }
                }
                else if (Rule.AccessControlType == AccessControlType.Allow)
                {
                    if (Rule.IsInherited)
                    {
                        perm.InheritedAllowRights |= Rule.FileSystemRights;
                    }
                    else
                    {
                        perm.AllowRights |= Rule.FileSystemRights;
                    }
                }
            }
            return perm;
        }
    }

    public class FileSystemAccessRule
    {
        public AccessControlType AccessControlType { get; set; }
        public FileSystemRights FileSystemRights { get; set; }
        public string IdentityReference { get; set; }
        public bool IsInherited { get; set; }
    }

    public enum AccessControlType
    {
        Allow = 0,
        Deny = 1
    }

    [Flags]
    public enum InheritanceFlags
    {
        None = 0,
        ContainerInherit = 1,
        ObjectInherit = 2
    }

    [Flags]
    public enum PropagationFlags
    {
        None = 0,
        NoPropagateInherit = 1,
        InheritOnly = 2
    }

    [Flags]
    public enum FileSystemRights
    {
        ReadData = 1,
        ListDirectory = 1,
        WriteData = 2,
        CreateFiles = 2,
        AppendData = 4,
        CreateDirectories = 4,
        ReadExtendedAttributes = 8,
        WriteExtendedAttributes = 16,
        ExecuteFile = 32,
        Traverse = 32,
        DeleteSubdirectoriesAndFiles = 64,
        ReadAttributes = 128,
        WriteAttributes = 256,
        Write = 278,
        Delete = 65536,
        ReadPermissions = 131072,
        Read = 131209,
        ReadAndExecute = 131241,
        Modify = 197055,
        ChangePermissions = 262144,
        TakeOwnership = 524288,
        Synchronize = 1048576,
        FullControl = 2032127
    }
}
