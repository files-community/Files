using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents an advanced ACE.
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
			FileSystemRights = (AccessMaskFlags)accessRule.FileSystemRights;
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

		private AccessMaskFlags fileSystemRights;
		public AccessMaskFlags FileSystemRights
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

		private List<AccessMaskItem> grantedPermissions;
		public List<AccessMaskItem> GrantedPermissions
		{
			get => grantedPermissions;
			set => SetProperty(ref grantedPermissions, value);
		}

		public bool WriteAccess => FileSystemRights.HasFlag(AccessMaskFlags.Write);
		public bool ReadAccess => FileSystemRights.HasFlag(AccessMaskFlags.Read);
		public bool ListDirectoryAccess => FileSystemRights.HasFlag(AccessMaskFlags.ListDirectory);
		public bool ReadAndExecuteAccess => FileSystemRights.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool ModifyAccess => FileSystemRights.HasFlag(AccessMaskFlags.Modify);
		public bool FullControlAccess => FileSystemRights.HasFlag(AccessMaskFlags.FullControl);
		public bool SpecialAccess
			=> (FileSystemRights &
					~AccessMaskFlags.Synchronize &
					(FullControlAccess ? ~AccessMaskFlags.FullControl : AccessMaskFlags.FullControl) &
					(ModifyAccess ? ~AccessMaskFlags.Modify : AccessMaskFlags.FullControl) &
					(ReadAndExecuteAccess ? ~AccessMaskFlags.ReadAndExecute : AccessMaskFlags.FullControl) &
					(ReadAccess ? ~AccessMaskFlags.Read : AccessMaskFlags.FullControl) &
					(WriteAccess ? ~AccessMaskFlags.Write : AccessMaskFlags.FullControl)) != 0;

		public RelayCommand<string> ChangeAccessControlTypeCommand { get; set; }
		public RelayCommand<string> ChangeInheritanceFlagsCommand { get; set; }
		#endregion

		#region Methods
		private List<AccessMaskItem> GetAllAccessMaskList()
		{
			// This list will be shown in an ACE item in security advanced page
			List<AccessMaskItem> accessControls;

			if (AreAdvancedPermissionsShown)
			{
				accessControls = new()
				{
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.FullControl,
						AccessMaskName = "SecurityFullControlLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.Traverse,
						AccessMaskName = "SecurityTraverseLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ExecuteFile,
						AccessMaskName = "SecurityExecuteFileLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ListDirectory,
						AccessMaskName = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ReadData,
						AccessMaskName = "SecurityReadDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ReadAttributes,
						AccessMaskName = "SecurityReadAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ReadExtendedAttributes,
						AccessMaskName = "SecurityReadExtendedAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.CreateFiles,
						AccessMaskName = "SecurityCreateFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.CreateDirectories,
						AccessMaskName = "SecurityCreateDirectoriesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.WriteData,
						AccessMaskName = "SecurityWriteDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.AppendData,
						AccessMaskName = "SecurityAppendDataLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.WriteAttributes,
						AccessMaskName = "SecurityWriteAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.WriteExtendedAttributes,
						AccessMaskName = "SecurityWriteExtendedAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.DeleteSubdirectoriesAndFiles,
						AccessMaskName = "SecurityDeleteSubdirectoriesAndFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.Delete,
						AccessMaskName = "Delete".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ReadPermissions,
						AccessMaskName = "SecurityReadPermissionsLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ChangePermissions,
						AccessMaskName = "SecurityChangePermissionsLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.TakeOwnership,
						AccessMaskName = "SecurityTakeOwnershipLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					}
				};

				if (IsFolder)
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.ExecuteFile ||
						x.AccessMask == AccessMaskFlags.ReadData ||
						x.AccessMask == AccessMaskFlags.WriteData ||
						x.AccessMask == AccessMaskFlags.AppendData);
				}
				else
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.Traverse ||
						x.AccessMask == AccessMaskFlags.ListDirectory ||
						x.AccessMask == AccessMaskFlags.CreateFiles ||
						x.AccessMask == AccessMaskFlags.CreateDirectories ||
						x.AccessMask == AccessMaskFlags.DeleteSubdirectoriesAndFiles);
				}
			}
			else
			{
				accessControls = new()
				{
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.FullControl,
						AccessMaskName = "SecurityFullControlLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.Modify,
						AccessMaskName = "SecurityModifyLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ReadAndExecute,
						AccessMaskName = "SecurityReadAndExecuteLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.ListDirectory,
						AccessMaskName = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.Read,
						AccessMaskName = "SecurityReadLabel/Text".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this)
					{
						AccessMask = AccessMaskFlags.Write,
						AccessMaskName = "Write".GetLocalizedResource(),
						IsEditable = !IsInherited
					},
					new AccessMaskItem(this, false)
					{
						AccessMaskName = "SecuritySpecialLabel/Text".GetLocalizedResource()
					}
				};

				if (!IsFolder)
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.ListDirectory);
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

			if (FileSystemRights == AccessMaskFlags.NULL)
			{
				accessMaskStrings.Add("None".GetLocalizedResource());
			}

			if (FullControlAccess)
			{
				accessMaskStrings.Add("SecurityFullControlLabel/Text".GetLocalizedResource());
			}
			else if (ModifyAccess)
			{
				accessMaskStrings.Add("SecurityModifyLabel/Text".GetLocalizedResource());
			}
			else if (ReadAndExecuteAccess)
			{
				accessMaskStrings.Add("SecurityReadAndExecuteLabel/Text".GetLocalizedResource());
			}
			else if (ReadAccess)
			{
				accessMaskStrings.Add("SecurityReadLabel/Text".GetLocalizedResource());
			}

			if (!FullControlAccess &&
				!ModifyAccess &&
				WriteAccess)
			{
				accessMaskStrings.Add("Write".GetLocalizedResource());
			}

			if (SpecialAccess)
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
