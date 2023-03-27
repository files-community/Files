using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an ACE.
	/// </summary>
	public class AccessControlEntry : ObservableObject
	{
		public readonly bool IsFolder;

		public string? PrincipalSid { get; set; }

		public Principal Principal { get; set; }

		private AccessControlType _accessControlType;
		public AccessControlType AccessControlType
		{
			get => _accessControlType;
			set
			{
				if (SetProperty(ref _accessControlType, value))
					OnPropertyChanged(nameof(AccessControlTypeGlyph));
			}
		}

		public string AccessControlTypeGlyph
			=> AccessControlType switch
			{
				AccessControlType.Allow => "\xE73E",
				_ => "\xF140" // AccessControlType.Deny
			};

		public bool IsInherited { get; set; }

		private InheritanceFlags _inheritanceFlags;
		public InheritanceFlags InheritanceFlags
		{
			get => _inheritanceFlags;
			set
			{
				if (SetProperty(ref _inheritanceFlags, value))
					OnPropertyChanged(nameof(InheritanceFlagsHumanized));
			}
		}

		private PropagationFlags _propagationFlags;
		public PropagationFlags PropagationFlags
		{
			get => _propagationFlags;
			set
			{
				if (SetProperty(ref _propagationFlags, value))
					OnPropertyChanged(nameof(InheritanceFlagsHumanized));
			}
		}

		private AccessMaskFlags _accessMaskFlags;
		public AccessMaskFlags AccessMaskFlags
		{
			get => _accessMaskFlags;
			set
			{
				if (SetProperty(ref _accessMaskFlags, value))
					OnPropertyChanged(nameof(AccessMaskFlagsHumanized));
			}
		}

		public string AccessMaskFlagsHumanized
		{
			get
			{
				var accessMaskStrings = new List<string>();

				if (AccessMaskFlags == AccessMaskFlags.NULL)
					accessMaskStrings.Add("None".GetLocalizedResource());

				if (FullControlAccess)
					accessMaskStrings.Add("SecurityFullControlLabel/Text".GetLocalizedResource());
				else if (ModifyAccess)
					accessMaskStrings.Add("SecurityModifyLabel/Text".GetLocalizedResource());
				else if (ReadAndExecuteAccess)
					accessMaskStrings.Add("SecurityReadAndExecuteLabel/Text".GetLocalizedResource());
				else if (ReadAccess)
					accessMaskStrings.Add("SecurityReadLabel/Text".GetLocalizedResource());

				if (!FullControlAccess && !ModifyAccess && WriteAccess)
					accessMaskStrings.Add("Write".GetLocalizedResource());

				if (SpecialAccess)
					accessMaskStrings.Add("SecuritySpecialLabel/Text".GetLocalizedResource());

				return string.Join(",", accessMaskStrings);
			}
		}

		public string IsInheritedHumanized
			=> IsInherited ? "Yes".GetLocalizedResource() : "No".GetLocalizedResource();

		public string InheritanceFlagsHumanized
		{
			get
			{
				var inheritanceStrings = new List<string>();

				if (PropagationFlags == PropagationFlags.None ||
					PropagationFlags == PropagationFlags.NoPropagateInherit)
					inheritanceStrings.Add("SecurityAdvancedFlagsFolderLabel".GetLocalizedResource());

				if (InheritanceFlags.HasFlag(InheritanceFlags.ContainerInherit))
					inheritanceStrings.Add("SecurityAdvancedFlagsSubfoldersLabel".GetLocalizedResource());

				if (InheritanceFlags.HasFlag(InheritanceFlags.ObjectInherit))
					inheritanceStrings.Add("SecurityAdvancedFlagsFilesLabel".GetLocalizedResource());

				// Capitalize first letter
				if (inheritanceStrings.Any())
					inheritanceStrings[0] = char.ToUpperInvariant(inheritanceStrings[0].First()) + inheritanceStrings[0][1..];

				return string.Join(",", inheritanceStrings);
			}
		}

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (SetProperty(ref _isSelected, value))
				{
					if (!value)
						AreAdvancedPermissionsShown = false;

					OnPropertyChanged(nameof(IsEditEnabled));
				}
			}
		}

		private bool _areAdvancedPermissionsShown;
		public bool AreAdvancedPermissionsShown
		{
			get => _areAdvancedPermissionsShown;
			set
			{
				// Reconstruct list
				if (SetProperty(ref _areAdvancedPermissionsShown, value))
					AccessMaskItemList = GetAllAccessMaskList();
			}
		}

		public bool IsEditEnabled
			=> IsSelected && !IsInherited;

		private List<AccessMaskItem> _accessMaskItemList;
		public List<AccessMaskItem> AccessMaskItemList
		{
			get => _accessMaskItemList;
			set => SetProperty(ref _accessMaskItemList, value);
		}

		public RelayCommand<string> ChangeAccessControlTypeCommand { get; set; }
		public RelayCommand<string> ChangeInheritanceFlagsCommand { get; set; }

		public AccessMaskFlags InheritedAllowAccessMaskFlags { get; set; }
		public AccessMaskFlags InheritedDenyAccessMaskFlags { get; set; }

		private AccessMaskFlags _allowedAccessMaskFlags;
		public AccessMaskFlags AllowedAccessMaskFlags
		{
			get => _allowedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _allowedAccessMaskFlags, value))
				{
					OnPropertyChanged(nameof(AllowedWriteAccess));
					OnPropertyChanged(nameof(AllowedFullControlAccess));
					OnPropertyChanged(nameof(AllowedListDirectoryAccess));
					OnPropertyChanged(nameof(AllowedModifyAccess));
					OnPropertyChanged(nameof(AllowedReadAccess));
					OnPropertyChanged(nameof(AllowedReadAndExecuteAccess));
				}
			}
		}

		private AccessMaskFlags _deniedAccessMaskFlags;
		public AccessMaskFlags DeniedAccessMaskFlags
		{
			get => _deniedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _deniedAccessMaskFlags, value))
				{
					OnPropertyChanged(nameof(DeniedWriteAccess));
					OnPropertyChanged(nameof(DeniedFullControlAccess));
					OnPropertyChanged(nameof(DeniedListDirectoryAccess));
					OnPropertyChanged(nameof(DeniedModifyAccess));
					OnPropertyChanged(nameof(DeniedReadAccess));
					OnPropertyChanged(nameof(DeniedReadAndExecuteAccess));
				}
			}
		}

		public bool WriteAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool ReadAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool ListDirectoryAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool ReadAndExecuteAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool ModifyAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool FullControlAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);
		public bool SpecialAccess
			=> (AccessMaskFlags &
				~AccessMaskFlags.Synchronize &
				(FullControlAccess ? ~AccessMaskFlags.FullControl : AccessMaskFlags.FullControl) &
				(ModifyAccess ? ~AccessMaskFlags.Modify : AccessMaskFlags.FullControl) &
				(ReadAndExecuteAccess ? ~AccessMaskFlags.ReadAndExecute : AccessMaskFlags.FullControl) &
				(ReadAccess ? ~AccessMaskFlags.Read : AccessMaskFlags.FullControl) &
				(WriteAccess ? ~AccessMaskFlags.Write : AccessMaskFlags.FullControl)) != 0;

		public bool AllowedInheritedWriteAccess =>          InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool AllowedInheritedReadAccess =>           InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool AllowedInheritedListDirectoryAccess =>  InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool AllowedInheritedReadAndExecuteAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool AllowedInheritedModifyAccess =>         InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool AllowedInheritedFullControlAccess =>    InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);

		public bool DeniedInheritedWriteAccess =>           InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool DeniedInheritedReadAccess =>            InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool DeniedInheritedListDirectoryAccess =>   InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool DeniedInheritedReadAndExecuteAccess =>  InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool DeniedInheritedModifyAccess =>          InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool DeniedInheritedFullControlAccess =>     InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);

		public bool AllowedWriteAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Write) || AllowedInheritedWriteAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Write, value);
		}

		public bool AllowedReadAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Read) || AllowedInheritedReadAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Read, value);
		}

		public bool AllowedListDirectoryAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory) || AllowedInheritedListDirectoryAccess;
			set => ToggleAllowAccess(AccessMaskFlags.ListDirectory, value);
		}

		public bool AllowedReadAndExecuteAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute) || AllowedInheritedReadAndExecuteAccess;
			set => ToggleAllowAccess(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool AllowedModifyAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Modify) || AllowedInheritedModifyAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Modify, value);
		}

		public bool AllowedFullControlAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl) || AllowedInheritedFullControlAccess;
			set => ToggleAllowAccess(AccessMaskFlags.FullControl, value);
		}

		public bool DeniedWriteAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Write) || DeniedInheritedWriteAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Write, value);
		}

		public bool DeniedReadAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Read) || DeniedInheritedReadAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Read, value);
		}

		public bool DeniedListDirectoryAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory) || DeniedInheritedListDirectoryAccess;
			set => ToggleDenyAccess(AccessMaskFlags.ListDirectory, value);
		}

		public bool DeniedReadAndExecuteAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute) || DeniedInheritedReadAndExecuteAccess;
			set => ToggleDenyAccess(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool DeniedModifyAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Modify) || DeniedInheritedModifyAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Modify, value);
		}

		public bool DeniedFullControlAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl) || DeniedInheritedFullControlAccess;
			set => ToggleDenyAccess(AccessMaskFlags.FullControl, value);
		}

		public AccessControlEntry(AccessControlEntryPrimitiveMapping accessRule, bool isFolder)
		{
			IsFolder = isFolder;

			AccessMaskItemList = GetAllAccessMaskList();

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

			AccessControlType = (AccessControlType)accessRule.AccessControlType;
			AccessMaskFlags = (AccessMaskFlags)accessRule.FileSystemRights;
			PrincipalSid = accessRule.PrincipalSid;
			Principal = Principal.FromSid(accessRule.PrincipalSid);
			IsInherited = accessRule.IsInherited;
			InheritanceFlags = (InheritanceFlags)accessRule.InheritanceFlags;
			PropagationFlags = (PropagationFlags)accessRule.PropagationFlags;

			switch (AccessControlType)
			{
				case AccessControlType.Allow:
					if (IsInherited)
						InheritedAllowAccessMaskFlags |= AccessMaskFlags;
					else
						AllowedAccessMaskFlags |= AccessMaskFlags;
					break;
				case AccessControlType.Deny:
					if (IsInherited)
						InheritedDenyAccessMaskFlags |= AccessMaskFlags;
					else
						DeniedAccessMaskFlags |= AccessMaskFlags;
					break;
			}
		}

		private void ToggleAllowAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value && !AllowedAccessMaskFlags.HasFlag(accessMask) && !InheritedAllowAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags |= accessMask;
				DeniedAccessMaskFlags &= ~accessMask;
			}
			else if (!value && AllowedAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags &= ~accessMask;
			}

			UpdateAccessControlEntry();
		}

		private void ToggleDenyAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value && !DeniedAccessMaskFlags.HasFlag(accessMask) && !InheritedDenyAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags |= accessMask;
				AllowedAccessMaskFlags &= ~accessMask;
			}
			else if (!value && DeniedAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags &= ~accessMask;
			}

			UpdateAccessControlEntry();
		}

		private void UpdateAccessControlEntry()
		{
		}

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
	}
}
