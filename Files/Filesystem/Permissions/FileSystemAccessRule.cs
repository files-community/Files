using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.Filesystem.Permissions
{
    public class FilePermissions
    {
        public string FilePath { get; set; }
        public bool IsFolder { get; set; }

        public bool CanReadFilePermissions { get; set; }

        public string OwnerSID { get; set; }

        public string CurrentUserSID { get; set; }

        public FilePermissions()
        {
            AccessRules = new List<FileSystemAccessRule>();
        }

        public List<FileSystemAccessRule> AccessRules { get; set; }
    }

    public class FilePermissionsManager
    {
        public string FilePath { get; set; }
        public bool IsFolder { get; set; }

        public bool CanReadFilePermissions { get; set; }

        public FilePermissionsManager(FilePermissions permissions)
        {
            FilePath = permissions.FilePath;
            IsFolder = permissions.IsFolder;
            CanReadFilePermissions = permissions.CanReadFilePermissions;
            Owner = UserGroup.FromSid(permissions.OwnerSID);
            CurrentUser = UserGroup.FromSid(permissions.CurrentUserSID);
            
            AccessRules = new ObservableCollection<FileSystemAccessRule>(permissions.AccessRules);
            RulesForUsers = new ObservableCollection<RulesForUser>(RulesForUser.ForAllUsers(AccessRules, IsFolder));
        }

        public UserGroup Owner { get; private set; }

        public UserGroup CurrentUser { get; private set; }

        public ObservableCollection<FileSystemAccessRule> AccessRules { get; set; }

        // Consolidated view 1       
        public ObservableCollection<RulesForUser> RulesForUsers { get; private set; }

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

        public FilePermissions ToFilePermissions()
        {
            return new FilePermissions()
            {
                FilePath = this.FilePath,
                IsFolder = this.IsFolder,
                AccessRules = this.AccessRules.ToList(),
                CanReadFilePermissions = this.CanReadFilePermissions,
                CurrentUserSID = this.CurrentUser.Sid,
                OwnerSID = this.Owner.Sid,
            };
        }
    }

    public class RulesForUser : ObservableObject
    {
        private bool isFolder;

        private ObservableCollection<FileSystemAccessRule> accessRules;

        public RulesForUser(ObservableCollection<FileSystemAccessRule> accessRules, bool isFolder)
        {
            this.accessRules = accessRules;
            this.isFolder = isFolder;
        }

        public void UpdateAccessRules()
        {
            foreach (var rule in accessRules.Where(x => x.IdentityReference == UserGroup.Sid && !x.IsInherited).ToList())
            {
                accessRules.Remove(rule);
            }
            if (AllowRights != 0 && !InheritedAllowRights.HasFlag(AllowRights)) // Do not set if permission is already granted by inheritance
            {
                accessRules.Add(new FileSystemAccessRule()
                {
                    AccessControlType = AccessControlType.Allow,
                    FileSystemRights = AllowRights,
                    IdentityReference = UserGroup.Sid,
                    InheritanceFlags = isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
                    PropagationFlags = PropagationFlags.None
                });
            }
            if (DenyRights != 0 && !InheritedDenyRights.HasFlag(DenyRights)) // Do not set if permission is already denied by inheritance
            {
                accessRules.Add(new FileSystemAccessRule()
                {
                    AccessControlType = AccessControlType.Deny,
                    FileSystemRights = DenyRights,
                    IdentityReference = UserGroup.Sid,
                    InheritanceFlags = isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
                    PropagationFlags = PropagationFlags.None
                });
            }
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

        private void ToggleAllowPermission(FileSystemRights permission, bool value)
        {
            if (value && !AllowRights.HasFlag(permission) && !InheritedAllowRights.HasFlag(permission))
            {
                AllowRights |= permission;
                DenyRights &= ~permission;
            }
            else if (!value && AllowRights.HasFlag(permission))
            {
                AllowRights &= ~permission;
            }
            UpdateAccessRules();
        }

        private void ToggleDenyPermission(FileSystemRights permission, bool value)
        {
            if (value && !DenyRights.HasFlag(permission) && !InheritedDenyRights.HasFlag(permission))
            {
                DenyRights |= permission;
                AllowRights &= ~permission;
            }
            else if (!value && DenyRights.HasFlag(permission))
            {
                DenyRights &= ~permission;
            }
            UpdateAccessRules();
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

        public static List<RulesForUser> ForAllUsers(ObservableCollection<FileSystemAccessRule> accessRules, bool isFolder)
        {
            return accessRules.Select(x => x.IdentityReference).Distinct().Select(x => RulesForUser.ForUser(accessRules, isFolder, x)).ToList();
        }

        public static RulesForUser ForUser(ObservableCollection<FileSystemAccessRule> accessRules, bool isFolder, string identity)
        {
            var perm = new RulesForUser(accessRules, isFolder) { UserGroup = UserGroup.FromSid(identity) };
            foreach (var Rule in accessRules.Where(x => x.IdentityReference == identity))
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
        public InheritanceFlags InheritanceFlags { get; set; }
        public PropagationFlags PropagationFlags { get; set; }
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
