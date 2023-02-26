using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Files.App.Filesystem.Permissions
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
				var parts = x.Split(',');

				InheritanceFlags = Enum.Parse<InheritanceFlags>(parts[0]);
				PropagationFlags = Enum.Parse<PropagationFlags>(parts[1]);
			});

			GrantedPermissions = GetGrantedPermissions();
		}

		public FileSystemAccessRuleForUI(FileSystemAccessRule2 accessRule, bool isFolder) : this(isFolder)
		{
			AccessControlType = (AccessControlType)accessRule.AccessControlType;
			FileSystemRights = (FileSystemRights)accessRule.FileSystemRights;
			IdentityReference = accessRule.IdentityReference;
			IsInherited = accessRule.IsInherited;
			InheritanceFlags = (InheritanceFlags)accessRule.InheritanceFlags;
			PropagationFlags = (PropagationFlags)accessRule.PropagationFlags;
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

		public UserGroup UserGroup
			=> UserGroup.FromSid(IdentityReference);

		public string Glyph
			=> AccessControlType switch
			{
				AccessControlType.Allow => "\xF13E",
				_ => "\xF140"
			};

		public string IsInheritedForUI
			=> IsInherited ? "Yes".GetLocalizedResource() : "No".GetLocalizedResource();

		public string FileSystemRightsForUI
			=> string.Join(", ", GetPermissionStrings());

		public string InheritanceFlagsForUI
			=> string.Join(", ", GetInheritanceStrings());

		private bool isSelected;
		public bool IsSelected
		{
			get => isSelected;
			set
			{
				if (SetProperty(ref isSelected, value))
				{
					if (!value)
						AreAdvancedPermissionsShown = false;

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
					GrantedPermissions = GetGrantedPermissions();
			}
		}

		public bool IsEditEnabled
			=> IsSelected && !IsInherited;

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
					Name = "SecurityFullControlLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				if (IsFolder)
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.Traverse,
						Name = "SecurityTraverseLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}
				else
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.ExecuteFile,
						Name = "SecurityExecuteFileLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}

				if (IsFolder)
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.ListDirectory,
						Name = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}
				else
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.ReadData,
						Name = "SecurityReadDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.ReadAttributes,
					Name = "SecurityReadAttributesLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.ReadExtendedAttributes,
					Name = "SecurityReadExtendedAttributesLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				if (IsFolder)
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.CreateFiles,
						Name = "SecurityCreateFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});

					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.CreateDirectories,
						Name = "SecurityCreateDirectoriesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}
				else
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.WriteData,
						Name = "SecurityWriteDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});

					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.AppendData,
						Name = "SecurityAppendDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.WriteAttributes,
					Name = "SecurityWriteAttributesLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.WriteExtendedAttributes,
					Name = "SecurityWriteExtendedAttributesLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				if (IsFolder)
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.DeleteSubdirectoriesAndFiles,
						Name = "SecurityDeleteSubdirectoriesAndFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.Delete,
					Name = "Delete".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.ReadPermissions,
					Name = "SecurityReadPermissionsLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.ChangePermissions,
					Name = "SecurityChangePermissionsLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.TakeOwnership,
					Name = "SecurityTakeOwnershipLabel/Text".GetLocalizedResource(),
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
					Name = "SecurityFullControlLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.Modify,
					Name = "SecurityModifyLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.ReadAndExecute,
					Name = "SecurityReadAndExecuteLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				if (IsFolder)
				{
					gpl.Add(new GrantedPermission(this)
					{
						Permission = FileSystemRights.ListDirectory,
						Name = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					});
				}

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.Read,
					Name = "SecurityReadLabel/Text".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new GrantedPermission(this)
				{
					Permission = FileSystemRights.Write,
					Name = "Write".GetLocalizedResource(),
					IsEditable = !IsInherited
				});

				gpl.Add(new SpecialPermission(this)
				{
					Name = "SecuritySpecialLabel/Text".GetLocalizedResource()
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
				return 
					(FileSystemRights &
					~FileSystemRights.Synchronize &
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
				ret.Add("SecurityAdvancedFlagsFolderLabel".GetLocalizedResource());
			}

			if (InheritanceFlags.HasFlag(InheritanceFlags.ContainerInherit))
			{
				ret.Add("SecurityAdvancedFlagsSubfoldersLabel".GetLocalizedResource());
			}

			if (InheritanceFlags.HasFlag(InheritanceFlags.ObjectInherit))
			{
				ret.Add("SecurityAdvancedFlagsFilesLabel".GetLocalizedResource());
			}

			if (ret.Any())
			{
				ret[0] = char.ToUpperInvariant(ret[0].First()) + ret[0].Substring(1);
			}

			return ret;
		}

		private IList<string> GetPermissionStrings()
		{
			var ret = new List<string>();

			if (FileSystemRights == 0)
			{
				ret.Add("None".GetLocalizedResource());
			}

			if (GrantsFullControl)
			{
				ret.Add("SecurityFullControlLabel/Text".GetLocalizedResource());
			}
			else if (GrantsModify)
			{
				ret.Add("SecurityModifyLabel/Text".GetLocalizedResource());
			}
			else if (GrantsReadAndExecute)
			{
				ret.Add("SecurityReadAndExecuteLabel/Text".GetLocalizedResource());
			}
			else if (GrantsRead)
			{
				ret.Add("SecurityReadLabel/Text".GetLocalizedResource());
			}

			if (!GrantsFullControl && !GrantsModify && GrantsWrite)
			{
				ret.Add("Write".GetLocalizedResource());
			}

			if (GrantsSpecial)
			{
				ret.Add("SecuritySpecialLabel/Text".GetLocalizedResource());
			}

			return ret;
		}

		public FileSystemAccessRule ToFileSystemAccessRule()
		{
			return new FileSystemAccessRule()
			{
				AccessControlType = AccessControlType,
				FileSystemRights = FileSystemRights,
				IdentityReference = IdentityReference,
				IsInherited = IsInherited,
				InheritanceFlags = InheritanceFlags,
				PropagationFlags = PropagationFlags
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
					TogglePermission(Permission, value);
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
			fsar = fileSystemAccessRule;
			fsar.PropertyChanged += Fsar_PropertyChanged;
		}

		protected virtual void Fsar_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "FileSystemRights")
				OnPropertyChanged(nameof(IsGranted));
		}
	}
}
