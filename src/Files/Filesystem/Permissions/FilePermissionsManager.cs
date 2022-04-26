using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.Filesystem.Permissions
{
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
            AreAccessRulesProtected = permissions.AreAccessRulesProtected;
            AccessRules = new ObservableCollection<FileSystemAccessRuleForUI>(permissions.AccessRules.Select(x => new FileSystemAccessRuleForUI(x, IsFolder)));
            RulesForUsers = new ObservableCollection<RulesForUser>(RulesForUser.ForAllUsers(AccessRules, IsFolder));
        }

        public UserGroup Owner { get; private set; }

        public UserGroup CurrentUser { get; private set; }

        public ObservableCollection<FileSystemAccessRuleForUI> AccessRules { get; set; }

        // Consolidated view 1
        public ObservableCollection<RulesForUser> RulesForUsers { get; private set; }

        public bool AreAccessRulesProtected { get; set; }

        public FileSystemRights GetEffectiveRights(UserGroup user)
        {
            var userSids = new List<string> { user.Sid };
            userSids.AddRange(user.Groups);

            FileSystemRights inheritedDenyRights = 0, denyRights = 0;
            FileSystemRights inheritedAllowRights = 0, allowRights = 0;

            foreach (FileSystemAccessRuleForUI Rule in AccessRules.Where(x => userSids.Contains(x.IdentityReference)))
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
                AccessRules = this.AccessRules.Select(x => x.ToFileSystemAccessRule()).ToList(),
                CanReadFilePermissions = this.CanReadFilePermissions,
                CurrentUserSID = this.CurrentUser.Sid,
                OwnerSID = this.Owner.Sid,
            };
        }
    }
}