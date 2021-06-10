using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.Filesystem.Permissions
{
    public class FileSystemAccessRuleForUI : ObservableObject
    {
        public FileSystemAccessRuleForUI(bool isFolder)
        {
            IsFolder = isFolder;
        }

        public FileSystemAccessRuleForUI(FileSystemAccessRule accessRule, bool isFolder)
        {
            AccessControlType = accessRule.AccessControlType;
            FileSystemRights = accessRule.FileSystemRights;
            IdentityReference = accessRule.IdentityReference;
            IsInherited = accessRule.IsInherited;
            InheritanceFlags = accessRule.InheritanceFlags;
            PropagationFlags = accessRule.PropagationFlags;
            IsFolder = isFolder;
        }

        public AccessControlType AccessControlType { get; set; }
        public string IdentityReference { get; set; }
        public bool IsInherited { get; set; }

        public InheritanceFlags inheritanceFlags;
        public InheritanceFlags InheritanceFlags
        {
            get => inheritanceFlags;
            set
            {
                if (SetProperty(ref inheritanceFlags, value))
                {
                    OnPropertyChanged(nameof(InheritanceFlagsForUI));
                }
            }
        }

        public PropagationFlags propagationFlags;
        public PropagationFlags PropagationFlags
        {
            get => propagationFlags;
            set
            {
                if (SetProperty(ref propagationFlags, value))
                {
                    OnPropertyChanged(nameof(InheritanceFlagsForUI));
                }
            }
        }

        public FileSystemRights fileSystemRights;
        public FileSystemRights FileSystemRights
        {
            get => fileSystemRights;
            set
            {
                if (SetProperty(ref fileSystemRights, value))
                {
                    OnPropertyChanged(nameof(GrantsWrite));
                    OnPropertyChanged(nameof(GrantsFullControl));
                    OnPropertyChanged(nameof(GrantsListDirectory));
                    OnPropertyChanged(nameof(GrantsModify));
                    OnPropertyChanged(nameof(GrantsRead));
                    OnPropertyChanged(nameof(GrantsReadAndExecute));
                    OnPropertyChanged(nameof(FileSystemRightsForUI));
                }
            }
        }

        public UserGroup UserGroup => UserGroup.FromSid(IdentityReference);

        public string Glyph => AccessControlType switch
        {
            AccessControlType.Allow => "\xF13E",
            _ => "\xF140"
        };

        public string IsInheritedForUI => IsInherited ? "SecurityAdvancedInheritedYesLabel".GetLocalized() : "SecurityAdvancedInheritedNoLabel".GetLocalized();

        public string FileSystemRightsForUI => string.Join(", ", GetPermissionStrings());

        public string InheritanceFlagsForUI => string.Join(", ", GetInheritanceStrings());

        public bool GrantsWrite => FileSystemRights.HasFlag(FileSystemRights.Write);
        public bool GrantsRead => FileSystemRights.HasFlag(FileSystemRights.Read);
        public bool GrantsListDirectory => FileSystemRights.HasFlag(FileSystemRights.ListDirectory);
        public bool GrantsReadAndExecute => FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute);
        public bool GrantsModify => FileSystemRights.HasFlag(FileSystemRights.Modify);
        public bool GrantsFullControl => FileSystemRights.HasFlag(FileSystemRights.FullControl);

        public bool IsFolder { get; }

        private IList<string> GetInheritanceStrings()
        {
            var ret = new List<string>();
            if (PropagationFlags == PropagationFlags.None || PropagationFlags == PropagationFlags.NoPropagateInherit)
            {
                ret.Add("SecurityAdvancedFlagsFolderLabel".GetLocalized());
            }
            if (InheritanceFlags.HasFlag(InheritanceFlags.ContainerInherit))
            {
                ret.Add("SecurityAdvancedFlagsSubfoldersLabel".GetLocalized());
            }
            if (InheritanceFlags.HasFlag(InheritanceFlags.ObjectInherit))
            {
                ret.Add("SecurityAdvancedFlagsFilesLabel".GetLocalized());
            }
            if (ret.Any())
            {
                ret[0] = ret[0].First().ToString().ToUpper() + ret[0].Substring(1);
            }
            return ret;
        }

        private IList<string> GetPermissionStrings()
        {
            var ret = new List<string>();
            if (FileSystemRights == 0)
            {
                return ret;
            }
            if (GrantsFullControl)
            {
                ret.Add("SecurityFullControlLabel/Text".GetLocalized());
            }
            else if (GrantsModify)
            {
                ret.Add("SecurityModifyLabel/Text".GetLocalized());
            }
            else if (GrantsReadAndExecute)
            {
                ret.Add("SecurityReadAndExecuteLabel/Text".GetLocalized());
            }
            else if (GrantsRead)
            {
                ret.Add("SecurityReadLabel/Text".GetLocalized());
            }
            if (!GrantsFullControl && !GrantsModify && GrantsWrite)
            {
                ret.Add("SecurityWriteLabel/Text".GetLocalized());
            }

            var otherRights = FileSystemRights & ~FileSystemRights.Synchronize &
                (GrantsFullControl ? ~FileSystemRights.FullControl : FileSystemRights.FullControl) &
                (GrantsModify ? ~FileSystemRights.Modify : FileSystemRights.FullControl) &
                (GrantsReadAndExecute ? ~FileSystemRights.ReadAndExecute : FileSystemRights.FullControl) &
                (GrantsRead ? ~FileSystemRights.Read : FileSystemRights.FullControl) &
                (GrantsWrite ? ~FileSystemRights.Write : FileSystemRights.FullControl);
            if (otherRights != 0)
            {
                ret.Add("SecuritySpecialLabel/Text".GetLocalized());
            }

            return ret;
        }

        public FileSystemAccessRule ToFileSystemAccessRule()
        {
            return new FileSystemAccessRule()
            {
                AccessControlType = this.AccessControlType,
                FileSystemRights = this.FileSystemRights,
                IdentityReference = this.IdentityReference,
                IsInherited = this.IsInherited,
                InheritanceFlags = this.InheritanceFlags,
                PropagationFlags = this.PropagationFlags
            };
        }
    }
}
