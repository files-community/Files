// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Represents an access control entry (ACE).
	/// </summary>
	public class AccessControlEntry : ObservableObject, IAccessControlEntry
	{
		/// <inheritdoc/>
		public string Path { get; private set; }

		/// <inheritdoc/>
		public bool IsFolder { get; private set; }

		/// <inheritdoc/>
		public Principal Principal { get; private set; }

		/// <inheritdoc/>
		public AccessControlEntryType AccessControlType { get; private set; }

		/// <inheritdoc/>
		public bool IsInherited { get; private set; }

		/// <summary>
		/// Gets the humanized string of <see cref="AccessControlType"/>.
		/// </summary>
		public string AccessControlTypeHumanized
			=> AccessControlType switch
			{
				AccessControlEntryType.Allow => "SecurityAllow".GetLocalizedResource(),
				_ => "SecurityDeny".GetLocalizedResource() // AccessControlType.Deny
			};

		/// <summary>
		/// Gets the icon glyph string of <see cref="AccessControlType"/>.
		/// </summary>
		public string AccessControlTypeGlyph
			=> AccessControlType switch
			{
				AccessControlEntryType.Allow => "\xE73E",
				_ => "\xF140" // AccessControlType.Deny
			};

		/// <summary>
		/// Gets the humanized string of <see cref="AccessMaskFlags"/>.
		/// </summary>
		public string AccessMaskFlagsHumanized
		{
			get
			{
				var accessMaskStrings = new List<string>();

				if (AccessMaskFlags == AccessMaskFlags.NULL)
					accessMaskStrings.Add("None".GetLocalizedResource());

				if (FullControlAccess)
					accessMaskStrings.Add("SecurityFullControl".GetLocalizedResource());
				else if (ModifyAccess)
					accessMaskStrings.Add("SecurityModify".GetLocalizedResource());
				else if (ReadAndExecuteAccess)
					accessMaskStrings.Add("SecurityReadAndExecute".GetLocalizedResource());
				else if (ReadAccess)
					accessMaskStrings.Add("SecurityRead".GetLocalizedResource());

				if (!FullControlAccess && !ModifyAccess && WriteAccess)
					accessMaskStrings.Add("SecurityWrite".GetLocalizedResource());

				if (SpecialAccess)
					accessMaskStrings.Add("SecuritySpecialPermissions".GetLocalizedResource());

				return string.Join(", ", accessMaskStrings);
			}
		}

		/// <summary>
		/// Gets the humanized string of <see cref="IsInherited"/>.
		/// </summary>
		public string IsInheritedHumanized
			=> IsInherited ? "Yes".GetLocalizedResource() : "No".GetLocalizedResource();

		/// <summary>
		/// Gets the humanized string of <see cref="AccessControlEntryFlags"/>.
		/// </summary>
		public string InheritanceFlagsHumanized
		{
			get
			{
				return AccessControlEntryFlags switch
				{
					AccessControlEntryFlags.None
						=> "SecurityAdvancedThisFolderOnly".GetLocalizedResource(),
					AccessControlEntryFlags.ObjectInherit when
						AccessControlEntryFlags.HasFlag(AccessControlEntryFlags.InheritOnly)
							=> "SecurityAdvancedFileOnly".GetLocalizedResource(),
					AccessControlEntryFlags.ObjectInherit when
						AccessControlEntryFlags.HasFlag(AccessControlEntryFlags.ContainerInherit)
							=> "SecurityAdvancedThisFolderAndFilesOnly".GetLocalizedResource(),
					AccessControlEntryFlags.ObjectInherit when
						AccessControlEntryFlags.HasFlag(AccessControlEntryFlags.ContainerInherit) &&
						AccessControlEntryFlags.HasFlag(AccessControlEntryFlags.InheritOnly)
							=> "SecurityAdvancedSubfoldersFilesOnly".GetLocalizedResource(),
					AccessControlEntryFlags.ObjectInherit
						=> "SecurityAdvancedFolderSubfoldersFiles".GetLocalizedResource(),
					AccessControlEntryFlags.ContainerInherit when
						AccessControlEntryFlags.HasFlag(AccessControlEntryFlags.InheritOnly)
							=> "SecurityAdvancedSubfoldersOnly",
					AccessControlEntryFlags.ContainerInherit
						=> "SecurityAdvancedFolderSubfolders".GetLocalizedResource(),
					_ => "None".GetLocalizedResource(),
				};
			}
		}

		private AccessMaskFlags _AccessMaskFlags;
		/// <inheritdoc/>
		public AccessMaskFlags AccessMaskFlags
		{
			get => _AccessMaskFlags;
			set
			{
				if (SetProperty(ref _AccessMaskFlags, value))
					OnPropertyChanged(nameof(AccessMaskFlagsHumanized));
			}
		}

		private AccessControlEntryFlags _InheritanceFlags;
		/// <inheritdoc/>
		public AccessControlEntryFlags AccessControlEntryFlags
		{
			get => _InheritanceFlags;
			set
			{
				if (SetProperty(ref _InheritanceFlags, value))
					OnPropertyChanged(nameof(InheritanceFlagsHumanized));
			}
		}

		private AccessMaskFlags _AllowedAccessMaskFlags;
		public AccessMaskFlags AllowedAccessMaskFlags
		{
			get => _AllowedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _AllowedAccessMaskFlags, value))
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

		private AccessMaskFlags _DeniedAccessMaskFlags;
		public AccessMaskFlags DeniedAccessMaskFlags
		{
			get => _DeniedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _DeniedAccessMaskFlags, value))
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

		private AccessMaskFlags InheritedAllowAccessMaskFlags { get; set; }

		private AccessMaskFlags InheritedDenyAccessMaskFlags { get; set; }

		#region Shoule be removed
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

		public bool AllowedInheritedWriteAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool AllowedInheritedReadAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool AllowedInheritedListDirectoryAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool AllowedInheritedReadAndExecuteAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool AllowedInheritedModifyAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool AllowedInheritedFullControlAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);

		public bool DeniedInheritedWriteAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool DeniedInheritedReadAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool DeniedInheritedListDirectoryAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool DeniedInheritedReadAndExecuteAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool DeniedInheritedModifyAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool DeniedInheritedFullControlAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);

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
		#endregion

		public AccessControlEntry(string path, bool isFolder, string ownerSid, AccessControlEntryType type, AccessMaskFlags accessMaskFlags, bool isInherited, AccessControlEntryFlags inheritanceFlags)
		{
			Path = path;
			IsFolder = isFolder;
			Principal = new(ownerSid);
			AccessControlType = type;
			AccessMaskFlags = accessMaskFlags;
			IsInherited = isInherited;
			AccessControlEntryFlags = inheritanceFlags;

			switch (AccessControlType)
			{
				case AccessControlEntryType.Allow:
					if (IsInherited)
						InheritedAllowAccessMaskFlags |= AccessMaskFlags;
					else
						AllowedAccessMaskFlags |= AccessMaskFlags;
					break;
				case AccessControlEntryType.Deny:
					if (IsInherited)
						InheritedDenyAccessMaskFlags |= AccessMaskFlags;
					else
						DeniedAccessMaskFlags |= AccessMaskFlags;
					break;
			}
		}

		private void ToggleAllowAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value &&
				!AllowedAccessMaskFlags.HasFlag(accessMask) &&
				!InheritedAllowAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags |= accessMask;
				DeniedAccessMaskFlags &= ~accessMask;
			}
			else if (!value &&
				AllowedAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags &= ~accessMask;
			}
		}

		private void ToggleDenyAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value &&
				!DeniedAccessMaskFlags.HasFlag(accessMask) &&
				!InheritedDenyAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags |= accessMask;
				AllowedAccessMaskFlags &= ~accessMask;
			}
			else if (!value &&
				DeniedAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags &= ~accessMask;
			}
		}
	}
}
