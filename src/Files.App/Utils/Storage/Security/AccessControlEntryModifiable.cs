// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Utils.Storage.Security
{
	public class AccessControlEntryModifiable : ObservableObject, IAccessControlEntry
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

		/// <inheritdoc/>
		public AccessMaskFlags AccessMaskFlags { get; private set; }

		/// <inheritdoc/>
		public AccessControlEntryFlags AccessControlEntryFlags { get; private set; }

		public string DialogTitle{ get; private set; }

		public IList<AccessControlEntryType> PossibleAccessControlTypes { get; private set; }

		public IList<string> PossibleAccessControlInheritanceAppliesToHumanizedTypes { get; private set; }

		public IList<AccessControlEntryFlags> PossibleAccessControlInheritanceAppliesToTypes { get; private set; }

		private AccessControlEntryType _SelectedAccessControlType;
		public AccessControlEntryType SelectedAccessControlType
		{
			get => _SelectedAccessControlType;
			set => SetProperty(ref _SelectedAccessControlType, value);
		}

		private int _SelectedAccessControlInheritanceAppliesToTypeIndex;
		public int SelectedAccessControlInheritanceAppliesToTypeIndex
		{
			get => _SelectedAccessControlInheritanceAppliesToTypeIndex;
			set
			{
				AccessControlEntryFlags = PossibleAccessControlInheritanceAppliesToTypes[value];

				SetProperty(ref _SelectedAccessControlInheritanceAppliesToTypeIndex, value);
			}
		}

		private bool _ShowAdvancedPermissions;
		public bool ShowAdvancedPermissions
		{
			get => _ShowAdvancedPermissions;
			set => SetProperty(ref _ShowAdvancedPermissions, value);
		}

		private string? _PermissionsVisibilityToggleLinkButtonContent;
		public string? PermissionsVisibilityToggleLinkButtonContent
		{
			get => _PermissionsVisibilityToggleLinkButtonContent;
			set => SetProperty(ref _PermissionsVisibilityToggleLinkButtonContent, value);
		}

		#region CheckBoxes
		public bool FullControlAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);
			set => UpdateAccessControl(AccessMaskFlags.FullControl, value);
		}

		public bool BasicModifyAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
			set => UpdateAccessControl(AccessMaskFlags.Modify, value);
		}

		public bool BasicReadAndExecuteAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
			set => UpdateAccessControl(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool BasicListFolderContentsAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
			set => UpdateAccessControl(AccessMaskFlags.ListDirectory, value);
		}

		public bool BasicReadAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.Read);
			set => UpdateAccessControl(AccessMaskFlags.Read, value);
		}

		public bool BasicWriteAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.Write);
			set => UpdateAccessControl(AccessMaskFlags.Write, value);
		}

		public bool BasicSpecialPermissionsAccessControl
		{
			get => (AccessMaskFlags &
				~AccessMaskFlags.Synchronize &
				(FullControlAccessControl ? ~AccessMaskFlags.FullControl : AccessMaskFlags.FullControl) &
				(BasicModifyAccessControl ? ~AccessMaskFlags.Modify : AccessMaskFlags.FullControl) &
				(BasicReadAndExecuteAccessControl ? ~AccessMaskFlags.ReadAndExecute : AccessMaskFlags.FullControl) &
				(BasicReadAccessControl ? ~AccessMaskFlags.Read : AccessMaskFlags.FullControl) &
				(BasicWriteAccessControl ? ~AccessMaskFlags.Write : AccessMaskFlags.FullControl)) != 0;
		}

		public bool AdvancedTraverseAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.Traverse);
			set => UpdateAccessControl(AccessMaskFlags.Traverse, value);
		}

		public bool AdvancedReadDataAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.ReadData);
			set => UpdateAccessControl(AccessMaskFlags.ReadData, value);
		}

		public bool AdvancedReadAttributesAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.ReadAttributes);
			set => UpdateAccessControl(AccessMaskFlags.ReadAttributes, value);
		}

		public bool AdvancedReadExtendedAttributesAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.ReadExtendedAttributes);
			set => UpdateAccessControl(AccessMaskFlags.ReadExtendedAttributes, value);
		}

		public bool AdvancedWriteDataAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.WriteData);
			set => UpdateAccessControl(AccessMaskFlags.WriteData, value);
		}

		public bool AdvancedAppendDataAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.AppendData);
			set => UpdateAccessControl(AccessMaskFlags.AppendData, value);
		}

		public bool AdvancedWriteAttributesAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.WriteAttributes);
			set => UpdateAccessControl(AccessMaskFlags.WriteAttributes, value);
		}

		public bool AdvancedWriteExtendedAttributesAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.WriteExtendedAttributes);
			set => UpdateAccessControl(AccessMaskFlags.WriteExtendedAttributes, value);
		}

		public bool AdvancedDeleteSubfoldersAndFilesAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.DeleteSubdirectoriesAndFiles);
			set => UpdateAccessControl(AccessMaskFlags.DeleteSubdirectoriesAndFiles, value);
		}

		public bool AdvancedDeleteAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.Delete);
			set => UpdateAccessControl(AccessMaskFlags.Delete, value);
		}

		public bool AdvancedReadPermissionsAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.ReadPermissions);
			set => UpdateAccessControl(AccessMaskFlags.ReadPermissions, value);
		}

		public bool AdvancedChangePermissionsAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.ChangePermissions);
			set => UpdateAccessControl(AccessMaskFlags.ChangePermissions, value);
		}

		public bool AdvancedTakeOwnershipAccessControl
		{
			get => AccessMaskFlags.HasFlag(AccessMaskFlags.TakeOwnership);
			set => UpdateAccessControl(AccessMaskFlags.TakeOwnership, value);
		}
		#endregion

		public ICommand TogglePermissionsVisibilityCommand;

		public AccessControlEntryModifiable(IAccessControlEntry item, bool isNew = false)
		{
			Path = item.Path;
			IsFolder = item.IsFolder;
			Principal = item.Principal;
			SelectedAccessControlType = item.AccessControlType;
			IsInherited = item.IsInherited;
			AccessMaskFlags = item.AccessMaskFlags;
			AccessControlEntryFlags = item.AccessControlEntryFlags;

			DialogTitle = isNew ? "SecurityAddPermission".GetLocalizedResource() : "SecurityEditPermission".GetLocalizedResource();
			ShowAdvancedPermissions = false;
			PermissionsVisibilityToggleLinkButtonContent = "SecurityShowAdvancedPermissions".GetLocalizedResource();
			TogglePermissionsVisibilityCommand = new RelayCommand(ExecuteTogglePermissionsVisibility);

			PossibleAccessControlTypes = new List<AccessControlEntryType>()
			{
				AccessControlEntryType.Allow,
				AccessControlEntryType.Deny,
			};

			PossibleAccessControlInheritanceAppliesToHumanizedTypes = new List<string>()
			{
				"SecurityAdvancedThisFolderOnly".GetLocalizedResource(),
				"SecurityAdvancedFolderAndSubfoldersAndFiles".GetLocalizedResource(),
				"SecurityAdvancedFolderAndSubfolders".GetLocalizedResource(),
				"SecurityAdvancedThisFolderAndFilesOnly".GetLocalizedResource(),
				"SecurityAdvancedSubfoldersFilesOnly".GetLocalizedResource(),
				"SecurityAdvancedSubfoldersOnly".GetLocalizedResource(),
				"SecurityAdvancedFileOnly".GetLocalizedResource(),
			};

			PossibleAccessControlInheritanceAppliesToTypes = new List<AccessControlEntryFlags>()
			{
				AccessControlEntryFlags.None,
				AccessControlEntryFlags.ObjectInherit,
				AccessControlEntryFlags.ContainerInherit,
				AccessControlEntryFlags.ObjectInherit & AccessControlEntryFlags.ContainerInherit,
				AccessControlEntryFlags.ObjectInherit & AccessControlEntryFlags.ContainerInherit & AccessControlEntryFlags.InheritOnly,
				AccessControlEntryFlags.ContainerInherit & AccessControlEntryFlags.InheritOnly, 
				AccessControlEntryFlags.ObjectInherit & AccessControlEntryFlags.InheritOnly, 
			};
		}

		private void ExecuteTogglePermissionsVisibility()
		{
			ShowAdvancedPermissions = !ShowAdvancedPermissions;
			PermissionsVisibilityToggleLinkButtonContent =
				ShowAdvancedPermissions
					? "SecurityShowBasicPermissions".GetLocalizedResource()
					: "SecurityShowAdvancedPermissions".GetLocalizedResource();
		}

		private void UpdateAccessControl(AccessMaskFlags mask, bool value)
		{
			if (value)
				AccessMaskFlags |= mask;
			else
				AccessMaskFlags &= ~mask;

			// Notify changes
			OnPropertyChanged(nameof(FullControlAccessControl));
			OnPropertyChanged(nameof(BasicModifyAccessControl));
			OnPropertyChanged(nameof(BasicReadAndExecuteAccessControl));
			OnPropertyChanged(nameof(BasicListFolderContentsAccessControl));
			OnPropertyChanged(nameof(BasicReadAccessControl));
			OnPropertyChanged(nameof(BasicWriteAccessControl));
			OnPropertyChanged(nameof(BasicSpecialPermissionsAccessControl));
			OnPropertyChanged(nameof(AdvancedTraverseAccessControl));
			OnPropertyChanged(nameof(AdvancedReadDataAccessControl));
			OnPropertyChanged(nameof(AdvancedReadAttributesAccessControl));
			OnPropertyChanged(nameof(AdvancedReadExtendedAttributesAccessControl));
			OnPropertyChanged(nameof(AdvancedWriteDataAccessControl));
			OnPropertyChanged(nameof(AdvancedAppendDataAccessControl));
			OnPropertyChanged(nameof(AdvancedWriteAttributesAccessControl));
			OnPropertyChanged(nameof(AdvancedWriteExtendedAttributesAccessControl));
			OnPropertyChanged(nameof(AdvancedDeleteSubfoldersAndFilesAccessControl));
			OnPropertyChanged(nameof(AdvancedDeleteAccessControl));
			OnPropertyChanged(nameof(AdvancedReadPermissionsAccessControl));
			OnPropertyChanged(nameof(AdvancedChangePermissionsAccessControl));
			OnPropertyChanged(nameof(AdvancedTakeOwnershipAccessControl));
		}

		public bool SaveChanges()
		{
			AccessControlHelper.UpdateAccessControlEntry(Path, this);

			return false;
		}
	}
}
