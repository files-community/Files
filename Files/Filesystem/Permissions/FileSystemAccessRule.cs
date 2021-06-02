using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem.Permissions
{
    public class FilePermissions
    {
        public string Path { get; set; }

        public FilePermissions()
        {
            AccessRules = new List<FileSystemAccessRule>();
        }

        public List<FileSystemAccessRule> AccessRules { get; set; }

        // Consolidated view 1
        public List<RulesForUser> RulesForUsers => RulesForUser.ForAllUsers(AccessRules);

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

    public class RulesForUser
    {
        public FileSystemRights InheritedDenyRights { get; set; }
        public FileSystemRights InheritedAllowRights { get; set; }
        public FileSystemRights DenyRights { get; set; }
        public FileSystemRights AllowRights { get; set; }
        public UserGroup UserGroup { get; set; }

        public bool GrantsInheritedWrite => InheritedAllowRights.HasFlag(FileSystemRights.Write);
        public bool GrantsInheritedRead => InheritedAllowRights.HasFlag(FileSystemRights.Read);
        public bool GrantsInheritedListDirectory => InheritedAllowRights.HasFlag(FileSystemRights.ListDirectory);
        public bool GrantsInheritedReadAndExecute => InheritedAllowRights.HasFlag(FileSystemRights.ReadAndExecute);
        public bool GrantsInheritedModify => InheritedAllowRights.HasFlag(FileSystemRights.Modify);
        public bool GrantsInheritedFullControl => InheritedAllowRights.HasFlag(FileSystemRights.FullControl);

        public bool GrantsWrite => AllowRights.HasFlag(FileSystemRights.Write) || GrantsInheritedWrite;
        public bool GrantsRead => AllowRights.HasFlag(FileSystemRights.Read) || GrantsInheritedRead;
        public bool GrantsListDirectory => AllowRights.HasFlag(FileSystemRights.ListDirectory) || GrantsInheritedListDirectory;
        public bool GrantsReadAndExecute => AllowRights.HasFlag(FileSystemRights.ReadAndExecute) || GrantsInheritedReadAndExecute;
        public bool GrantsModify => AllowRights.HasFlag(FileSystemRights.Modify) || GrantsInheritedModify;
        public bool GrantsFullControl => AllowRights.HasFlag(FileSystemRights.FullControl) || GrantsInheritedFullControl;

        public bool DeniesInheritedWrite => InheritedDenyRights.HasFlag(FileSystemRights.Write);
        public bool DeniesInheritedRead => InheritedDenyRights.HasFlag(FileSystemRights.Read);
        public bool DeniesInheritedListDirectory => InheritedDenyRights.HasFlag(FileSystemRights.ListDirectory);
        public bool DeniesInheritedReadAndExecute => InheritedDenyRights.HasFlag(FileSystemRights.ReadAndExecute);
        public bool DeniesInheritedModify => InheritedDenyRights.HasFlag(FileSystemRights.Modify);
        public bool DeniesInheritedFullControl => InheritedDenyRights.HasFlag(FileSystemRights.FullControl);

        public bool DeniesWrite => DenyRights.HasFlag(FileSystemRights.Write) || DeniesInheritedWrite;
        public bool DeniesRead => DenyRights.HasFlag(FileSystemRights.Read) || DeniesInheritedRead;
        public bool DeniesListDirectory => DenyRights.HasFlag(FileSystemRights.ListDirectory) || DeniesInheritedListDirectory;
        public bool DeniesReadAndExecute => DenyRights.HasFlag(FileSystemRights.ReadAndExecute) || DeniesInheritedReadAndExecute;
        public bool DeniesModify => DenyRights.HasFlag(FileSystemRights.Modify) || DeniesInheritedModify;
        public bool DeniesFullControl => DenyRights.HasFlag(FileSystemRights.FullControl) || DeniesInheritedFullControl;

        public static List<RulesForUser> ForAllUsers(IEnumerable<FileSystemAccessRule> rules)
        {
            return rules.Select(x => x.IdentityReference).Distinct().Select(x => RulesForUser.ForUser(rules, x)).ToList();
        }

        public static RulesForUser ForUser(IEnumerable<FileSystemAccessRule> rules, string identity)
        {
            var perm = new RulesForUser() { UserGroup = UserGroup.FromSid(identity) };
            foreach (var Rule in rules.Where(x => x.IdentityReference == identity))
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
