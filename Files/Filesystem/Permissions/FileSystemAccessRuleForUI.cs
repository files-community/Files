using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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

            ChangeAccessControlTypeCommand = new RelayCommand<string>(x =>
            {
                AccessControlType = Enum.Parse<AccessControlType>(x);
            });

            ChangeInheritanceFlagsCommand = new RelayCommand<string>(x =>
            {
                InheritanceFlags = Enum.Parse<InheritanceFlags>(x.Split(',')[0]);
                PropagationFlags = Enum.Parse<PropagationFlags>(x.Split(',')[1]);
            });
        }

        public FileSystemAccessRuleForUI(FileSystemAccessRule accessRule, bool isFolder): this(isFolder)
        {
            AccessControlType = accessRule.AccessControlType;
            FileSystemRights = accessRule.FileSystemRights;
            IdentityReference = accessRule.IdentityReference;
            IsInherited = accessRule.IsInherited;
            InheritanceFlags = accessRule.InheritanceFlags;
            PropagationFlags = accessRule.PropagationFlags;
        }

        public RelayCommand<string> ChangeAccessControlTypeCommand { get; set; }
        public RelayCommand<string> ChangeInheritanceFlagsCommand { get; set; }

        private AccessControlType accessControlType;
        public AccessControlType AccessControlType
        {
            get => accessControlType;
            set
            {
                if (SetProperty(ref accessControlType, value))
                {
                    OnPropertyChanged(nameof(Glyph));
                }
            }
        }

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
                    OnPropertyChanged(nameof(GrantsSpecial));
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

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (SetProperty(ref isSelected, value))
                {
                    OnPropertyChanged(nameof(IsEditEnabled));
                }
            }
        }

        public bool IsEditEnabled => IsSelected && !IsInherited;

        private void TogglePermission(FileSystemRights permission, bool value)
        {
            if (value && !FileSystemRights.HasFlag(permission))
            {
                FileSystemRights |= permission;
            }
            else if (!value && FileSystemRights.HasFlag(permission))
            {
                FileSystemRights &= ~permission;
            }
        }

        public bool GrantsWrite
        {
            get => FileSystemRights.HasFlag(FileSystemRights.Write);
            set => TogglePermission(FileSystemRights.Write, value);
        }
        public bool GrantsRead
        {
            get => FileSystemRights.HasFlag(FileSystemRights.Read);
            set => TogglePermission(FileSystemRights.Read, value);
        }
        public bool GrantsListDirectory
        {
            get => FileSystemRights.HasFlag(FileSystemRights.ListDirectory);
            set => TogglePermission(FileSystemRights.ListDirectory, value);
        }
        public bool GrantsReadAndExecute
        {
            get => FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute);
            set => TogglePermission(FileSystemRights.ReadAndExecute, value);
        }
        public bool GrantsModify
        {
            get => FileSystemRights.HasFlag(FileSystemRights.Modify);
            set => TogglePermission(FileSystemRights.Modify, value);
        }

        public bool GrantsFullControl
        {
            get => FileSystemRights.HasFlag(FileSystemRights.FullControl);
            set => TogglePermission(FileSystemRights.FullControl, value);
        }
        public bool GrantsSpecial
        {
            get
            {
                return (FileSystemRights & ~FileSystemRights.Synchronize &
                    (GrantsFullControl ? ~FileSystemRights.FullControl : FileSystemRights.FullControl) &
                    (GrantsModify ? ~FileSystemRights.Modify : FileSystemRights.FullControl) &
                    (GrantsReadAndExecute ? ~FileSystemRights.ReadAndExecute : FileSystemRights.FullControl) &
                    (GrantsRead ? ~FileSystemRights.Read : FileSystemRights.FullControl) &
                    (GrantsWrite ? ~FileSystemRights.Write : FileSystemRights.FullControl)) != 0;
            }
        }

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
            if (GrantsSpecial)
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
