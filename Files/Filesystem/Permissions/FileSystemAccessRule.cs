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
        public List<FileSystemAccessRule> AccessRules { get; set; }

        public FilePermissions()
        {
            AccessRules = new List<FileSystemAccessRule>();
        }

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

    public class FileSystemAccessRule
    {
        public AccessControlType AccessControlType { get; set; }
        public FileSystemRights FileSystemRights { get; set; }
        public string IdentityReference { get; set; }
        public bool IsInherited { get; set; }

        public bool CanRead => FileSystemRights.HasFlag(FileSystemRights.Read);
        public bool CanWrite => FileSystemRights.HasFlag(FileSystemRights.Write);
        public bool CanModify => FileSystemRights.HasFlag(FileSystemRights.Modify);
        public bool CanReadAndExecute => FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute);
        public bool CanListDirectory => FileSystemRights.HasFlag(FileSystemRights.ListDirectory);
        public bool CanTakeOwnership => FileSystemRights.HasFlag(FileSystemRights.TakeOwnership);
        public bool HasFullControl => FileSystemRights.HasFlag(FileSystemRights.FullControl);
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
