using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            GrantedPermissions = GetGrantedPermissions();
        }

        public FileSystemAccessRuleForUI(FileSystemAccessRule accessRule, bool isFolder) : this(isFolder)
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

        private InheritanceFlags inheritanceFlags;

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

        private PropagationFlags propagationFlags;

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

        private FileSystemRights fileSystemRights;

        public FileSystemRights FileSystemRights
        {
            get => fileSystemRights;
            set
            {
                if (SetProperty(ref fileSystemRights, value))
                {
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

        public string IsInheritedForUI => IsInherited ? "Yes".GetLocalized() : "SecurityAdvancedInheritedNoLabel".GetLocalized();

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
                    if (!value)
                    {
                        AreAdvancedPermissionsShown = false;
                    }
                    OnPropertyChanged(nameof(IsEditEnabled));
                }
            }
        }

        private bool areAdvancedPermissionsShown;

        public bool AreAdvancedPermissionsShown
        {
            get => areAdvancedPermissionsShown;
            set
            {
                if (SetProperty(ref areAdvancedPermissionsShown, value))
                {
                    GrantedPermissions = GetGrantedPermissions();
                }
            }
        }

        public bool IsEditEnabled => IsSelected && !IsInherited;

        private List<GrantedPermission> grantedPermissions;

        public List<GrantedPermission> GrantedPermissions
        {
            get => grantedPermissions;
            set => SetProperty(ref grantedPermissions, value);
        }

        private List<GrantedPermission> GetGrantedPermissions()
        {
            if (AreAdvancedPermissionsShown)
            {
                var gpl = new List<GrantedPermission>();
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.FullControl,
                    Name = "SecurityFullControlLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                if (IsFolder)
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.Traverse,
                        Name = "SecurityTraverseLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                else
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.ExecuteFile,
                        Name = "SecurityExecuteFileLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                if (IsFolder)
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.ListDirectory,
                        Name = "SecurityListDirectoryLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                else
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.ReadData,
                        Name = "SecurityReadDataLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.ReadAttributes,
                    Name = "SecurityReadAttributesLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.ReadExtendedAttributes,
                    Name = "SecurityReadExtendedAttributesLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                if (IsFolder)
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.CreateFiles,
                        Name = "SecurityCreateFilesLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.CreateDirectories,
                        Name = "SecurityCreateDirectoriesLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                else
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.WriteData,
                        Name = "SecurityWriteDataLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.AppendData,
                        Name = "SecurityAppendDataLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.WriteAttributes,
                    Name = "SecurityWriteAttributesLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.WriteExtendedAttributes,
                    Name = "SecurityWriteExtendedAttributesLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                if (IsFolder)
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.DeleteSubdirectoriesAndFiles,
                        Name = "SecurityDeleteSubdirectoriesAndFilesLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.Delete,
                    Name = "Delete".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.ReadPermissions,
                    Name = "SecurityReadPermissionsLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.ChangePermissions,
                    Name = "SecurityChangePermissionsLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.TakeOwnership,
                    Name = "SecurityTakeOwnershipLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                return gpl;
            }
            else
            {
                var gpl = new List<GrantedPermission>();
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.FullControl,
                    Name = "SecurityFullControlLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.Modify,
                    Name = "SecurityModifyLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.ReadAndExecute,
                    Name = "SecurityReadAndExecuteLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                if (IsFolder)
                {
                    gpl.Add(new GrantedPermission(this)
                    {
                        Permission = FileSystemRights.ListDirectory,
                        Name = "SecurityListDirectoryLabel/Text".GetLocalized(),
                        IsEditable = !IsInherited
                    });
                }
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.Read,
                    Name = "SecurityReadLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new GrantedPermission(this)
                {
                    Permission = FileSystemRights.Write,
                    Name = "SecurityWriteLabel/Text".GetLocalized(),
                    IsEditable = !IsInherited
                });
                gpl.Add(new SpecialPermission(this)
                {
                    Name = "SecuritySpecialLabel/Text".GetLocalized()
                });
                return gpl;
            }
        }

        public bool GrantsWrite => FileSystemRights.HasFlag(FileSystemRights.Write);
        public bool GrantsRead => FileSystemRights.HasFlag(FileSystemRights.Read);
        public bool GrantsListDirectory => FileSystemRights.HasFlag(FileSystemRights.ListDirectory);
        public bool GrantsReadAndExecute => FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute);
        public bool GrantsModify => FileSystemRights.HasFlag(FileSystemRights.Modify);
        public bool GrantsFullControl => FileSystemRights.HasFlag(FileSystemRights.FullControl);

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
                ret.Add("SecurityNoneLabel/Text".GetLocalized());
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

    public class SpecialPermission : GrantedPermission
    {
        private bool isGranted;

        public override bool IsGranted
        {
            get => fsar.GrantsSpecial;
            set => SetProperty(ref isGranted, value);
        }

        public SpecialPermission(FileSystemAccessRuleForUI fileSystemAccessRule)
            : base(fileSystemAccessRule)
        {
            IsEditable = false;
        }

        protected override void Fsar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileSystemRights")
            {
                OnPropertyChanged(nameof(IsGranted));
            }
        }
    }

    public class GrantedPermission : ObservableObject
    {
        protected FileSystemAccessRuleForUI fsar;

        public virtual bool IsGranted
        {
            get => fsar.FileSystemRights.HasFlag(Permission);
            set
            {
                if (IsEditable)
                {
                    TogglePermission(Permission, value);
                }
            }
        }

        public string Name { get; set; }

        public bool IsEditable { get; set; }

        public FileSystemRights Permission { get; set; }

        private void TogglePermission(FileSystemRights permission, bool value)
        {
            if (value && !fsar.FileSystemRights.HasFlag(permission))
            {
                fsar.FileSystemRights |= permission;
            }
            else if (!value && fsar.FileSystemRights.HasFlag(permission))
            {
                fsar.FileSystemRights &= ~permission;
            }
        }

        public GrantedPermission(FileSystemAccessRuleForUI fileSystemAccessRule)
        {
            this.fsar = fileSystemAccessRule;
            this.fsar.PropertyChanged += Fsar_PropertyChanged;
        }

        protected virtual void Fsar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileSystemRights")
            {
                OnPropertyChanged(nameof(IsGranted));
            }
        }
    }
}