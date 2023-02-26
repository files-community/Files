using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents an advanced ACE information
	/// </summary>
	public class AccessControlEntryAdvanced : ObservableObject
	{
		public AccessControlEntryAdvanced(bool isFolder)
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

			grantedPermissions = new();
			GrantedPermissions = GetAllAccessMaskList();
		}

		public AccessControlEntryAdvanced(FileSystemAccessRule2 accessRule, bool isFolder)
			: this(isFolder)
		{
			AccessControlType = (AccessControlType)accessRule.AccessControlType;
			FileSystemRights = (AccessMask)accessRule.FileSystemRights;
			PrincipalSid = accessRule.IdentityReference;
			IsInherited = accessRule.IsInherited;
			InheritanceFlags = (InheritanceFlags)accessRule.InheritanceFlags;
			PropagationFlags = (PropagationFlags)accessRule.PropagationFlags;
		}

		#region Fields, Properties, Commands
		public Principal Principal
			=> Principal.FromSid(PrincipalSid);

		public string AccessControlTypeGlyph
			=> AccessControlType switch
			{
				AccessControlType.Allow => "\xF13E",
				AccessControlType.Deny => "\xF140",
				_ => "\xF140"
			};

		private AccessControlType accessControlType;
		public AccessControlType AccessControlType
		{
			get => accessControlType;
			set
			{
				if (SetProperty(ref accessControlType, value))
				{
					OnPropertyChanged(nameof(AccessControlTypeGlyph));
				}
			}
		}

		public string? PrincipalSid { get; set; }

		public bool IsInherited { get; set; }

		private InheritanceFlags inheritanceFlags;
		public InheritanceFlags InheritanceFlags
		{
			get => inheritanceFlags;
			set
			{
				if (SetProperty(ref inheritanceFlags, value))
					OnPropertyChanged(nameof(InheritanceFlagsForUI));
			}
		}

		private PropagationFlags propagationFlags;
		public PropagationFlags PropagationFlags
		{
			get => propagationFlags;
			set
			{
				if (SetProperty(ref propagationFlags, value))
					OnPropertyChanged(nameof(InheritanceFlagsForUI));
			}
		}

		private AccessMask fileSystemRights;
		public AccessMask FileSystemRights
		{
			get => fileSystemRights;
			set
			{
				if (SetProperty(ref fileSystemRights, value))
					OnPropertyChanged(nameof(FileSystemRightsForUI));
			}
		}

		public string FileSystemRightsForUI
			=> string.Join(", ", GetAccessMaskStrings());

		public string IsInheritedForUI
			=> IsInherited ? "Yes".GetLocalizedResource() : "No".GetLocalizedResource();

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
					GrantedPermissions = GetAllAccessMaskList();
			}
		}

		public bool IsEditEnabled
			=> IsSelected && !IsInherited;

		public bool IsFolder { get; }

		private List<GrantedPermission> grantedPermissions;
		public List<GrantedPermission> GrantedPermissions
		{
			get => grantedPermissions;
			set => SetProperty(ref grantedPermissions, value);
		}

		public RelayCommand<string> ChangeAccessControlTypeCommand { get; set; }
		public RelayCommand<string> ChangeInheritanceFlagsCommand { get; set; }

		private bool GrantsWrite => FileSystemRights.HasFlag(AccessMask.Write);
		private bool GrantsRead => FileSystemRights.HasFlag(AccessMask.Read);
		private bool GrantsListDirectory => FileSystemRights.HasFlag(AccessMask.ListDirectory);
		private bool GrantsReadAndExecute => FileSystemRights.HasFlag(AccessMask.ReadAndExecute);
		private bool GrantsModify => FileSystemRights.HasFlag(AccessMask.Modify);
		private bool GrantsFullControl => FileSystemRights.HasFlag(AccessMask.FullControl);
		private bool GrantsSpecial
			=> (FileSystemRights &
					~AccessMask.Synchronize &
					(GrantsFullControl ? ~AccessMask.FullControl : AccessMask.FullControl) &
					(GrantsModify ? ~AccessMask.Modify : AccessMask.FullControl) &
					(GrantsReadAndExecute ? ~AccessMask.ReadAndExecute : AccessMask.FullControl) &
					(GrantsRead ? ~AccessMask.Read : AccessMask.FullControl) &
					(GrantsWrite ? ~AccessMask.Write : AccessMask.FullControl)) != 0;
		#endregion

		#region Methods
		private List<GrantedPermission> GetAllAccessMaskList()
		{
			// This list will be shown in an ACE item in security advanced page
			List<GrantedPermission> accessControls;

			if (AreAdvancedPermissionsShown)
			{
				accessControls = new()
				{
					new GrantedPermission(this)
					{
						Permission = AccessMask.FullControl,
						Name = "SecurityFullControlLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.Traverse,
						Name = "SecurityTraverseLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ExecuteFile,
						Name = "SecurityExecuteFileLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ListDirectory,
						Name = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ReadData,
						Name = "SecurityReadDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ReadAttributes,
						Name = "SecurityReadAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ReadExtendedAttributes,
						Name = "SecurityReadExtendedAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.CreateFiles,
						Name = "SecurityCreateFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.CreateDirectories,
						Name = "SecurityCreateDirectoriesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.WriteData,
						Name = "SecurityWriteDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.AppendData,
						Name = "SecurityAppendDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.WriteAttributes,
						Name = "SecurityWriteAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.WriteExtendedAttributes,
						Name = "SecurityWriteExtendedAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.DeleteSubdirectoriesAndFiles,
						Name = "SecurityDeleteSubdirectoriesAndFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.Delete,
						Name = "Delete".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ReadPermissions,
						Name = "SecurityReadPermissionsLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ChangePermissions,
						Name = "SecurityChangePermissionsLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.TakeOwnership,
						Name = "SecurityTakeOwnershipLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					}
				};

				if (IsFolder)
				{
					accessControls.RemoveAll(x =>
						x.Permission == AccessMask.ExecuteFile ||
						x.Permission == AccessMask.ReadData ||
						x.Permission == AccessMask.WriteData ||
						x.Permission == AccessMask.AppendData);
				}
				else
				{
					accessControls.RemoveAll(x =>
						x.Permission == AccessMask.Traverse ||
						x.Permission == AccessMask.ListDirectory ||
						x.Permission == AccessMask.CreateFiles ||
						x.Permission == AccessMask.CreateDirectories ||
						x.Permission == AccessMask.DeleteSubdirectoriesAndFiles);
				}
			}
			else
			{
				accessControls = new()
				{
					new GrantedPermission(this)
					{
						Permission = AccessMask.FullControl,
						Name = "SecurityFullControlLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.Modify,
						Name = "SecurityModifyLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ReadAndExecute,
						Name = "SecurityReadAndExecuteLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.ListDirectory,
						Name = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.Read,
						Name = "SecurityReadLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new GrantedPermission(this)
					{
						Permission = AccessMask.Write,
						Name = "Write".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new SpecialPermission(this)
					{
						Name = "SecuritySpecialLabel/Text".GetLocalizedResource()
					}
				};

				if (!IsFolder)
				{
					accessControls.RemoveAll(x =>
						x.Permission == AccessMask.ListDirectory);
				}
			}

			return accessControls;
		}

		private IList<string> GetInheritanceStrings()
		{
			var inheritanceStrings = new List<string>();

			if (PropagationFlags == PropagationFlags.None ||
				PropagationFlags == PropagationFlags.NoPropagateInherit)
			{
				inheritanceStrings.Add("SecurityAdvancedFlagsFolderLabel".GetLocalizedResource());
			}

			if (InheritanceFlags.HasFlag(InheritanceFlags.ContainerInherit))
			{
				inheritanceStrings.Add("SecurityAdvancedFlagsSubfoldersLabel".GetLocalizedResource());
			}

			if (InheritanceFlags.HasFlag(InheritanceFlags.ObjectInherit))
			{
				inheritanceStrings.Add("SecurityAdvancedFlagsFilesLabel".GetLocalizedResource());
			}

			if (inheritanceStrings.Any())
			{
				// Capitalize first letter
				inheritanceStrings[0] = char.ToUpperInvariant(inheritanceStrings[0].First()) + inheritanceStrings[0][1..];
			}

			return inheritanceStrings;
		}

		private IList<string> GetAccessMaskStrings()
		{
			var accessMaskStrings = new List<string>();

			if (FileSystemRights == AccessMask.NULL)
			{
				accessMaskStrings.Add("None".GetLocalizedResource());
			}

			if (GrantsFullControl)
			{
				accessMaskStrings.Add("SecurityFullControlLabel/Text".GetLocalizedResource());
			}
			else if (GrantsModify)
			{
				accessMaskStrings.Add("SecurityModifyLabel/Text".GetLocalizedResource());
			}
			else if (GrantsReadAndExecute)
			{
				accessMaskStrings.Add("SecurityReadAndExecuteLabel/Text".GetLocalizedResource());
			}
			else if (GrantsRead)
			{
				accessMaskStrings.Add("SecurityReadLabel/Text".GetLocalizedResource());
			}

			if (!GrantsFullControl &&
				!GrantsModify &&
				GrantsWrite)
			{
				accessMaskStrings.Add("Write".GetLocalizedResource());
			}

			if (GrantsSpecial)
			{
				accessMaskStrings.Add("SecuritySpecialLabel/Text".GetLocalizedResource());
			}

			return accessMaskStrings;
		}

		public FileSystemAccessRule ToFileSystemAccessRule()
		{
			return new FileSystemAccessRule()
			{
				AccessControlType = AccessControlType,
				FileSystemRights = FileSystemRights,
				IdentityReference = PrincipalSid,
				IsInherited = IsInherited,
				InheritanceFlags = InheritanceFlags,
				PropagationFlags = PropagationFlags
			};
		}
		#endregion
	}
}
