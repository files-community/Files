using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace FilesFullTrust
{
    public class FilePermissions
    {
        public string Path { get; set; }

        public bool CanReadFilePermissions { get; set; }

        public FilePermissions()
        {
            AccessRules = new List<FileSystemAccessRule2>();
        }

        public List<FileSystemAccessRule2> AccessRules { get; set; }
    }

    public class FileSystemAccessRule2
    {
        public AccessControlType AccessControlType { get; set; }
        public FileSystemRights FileSystemRights { get; set; }
        public string IdentityReference { get; set; }
        public bool IsInherited { get; set; }

        public static FileSystemAccessRule2 FromFileSystemAccessRule(FileSystemAccessRule rule)
        {
            var fsa2 = new FileSystemAccessRule2();
            fsa2.AccessControlType = rule.AccessControlType;
            fsa2.FileSystemRights = rule.FileSystemRights;
            fsa2.IsInherited = rule.IsInherited;
            fsa2.IdentityReference = rule.IdentityReference.Value;
            return fsa2;
        }
    }
}
