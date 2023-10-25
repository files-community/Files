// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Utils.Storage.Security
{
	public class AccessControlEntryModifiable : ObservableObject, IAccessControlEntry
	{
		private IAccessControlEntry _defaultItem;

		/// <inheritdoc/>
		public bool IsFolder { get; private set; }

		/// <inheritdoc/>
		public Principal Principal { get; private set; }

		/// <inheritdoc/>
		public AccessControlEntryType AccessControlType { get; private set; }

		/// <inheritdoc/>
		public bool IsInherited { get; private set; }

		public AccessMaskFlags AccessMaskFlags { get; private set; }

		public AccessControlEntryFlags AccessControlEntryFlags { get; private set; }

		public IList<AccessControlEntryType> PossibleAccessControlTypes { get; private set; }

		public IList<string> PossibleAccessControlInheritanceAppliesToTypes { get; private set; }

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
			set => SetProperty(ref _SelectedAccessControlInheritanceAppliesToTypeIndex, value);
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

		public bool AdvancedTakeOwnershipAccessControl => AccessMaskFlags.HasFlag(AccessMaskFlags.TakeOwnership);


		public ICommand TogglePermissionsVisibilityCommand;

		public AccessControlEntryModifiable(IAccessControlEntry item)
		{
			_defaultItem = item;
			IsFolder = item.IsFolder;
			Principal = item.Principal;
			SelectedAccessControlType = item.AccessControlType;
			IsInherited = item.IsInherited;
			AccessMaskFlags = item.AccessMaskFlags;
			AccessControlEntryFlags = item.AccessControlEntryFlags;

			ShowAdvancedPermissions = false;
			PermissionsVisibilityToggleLinkButtonContent = "Show advanced permissions";
			TogglePermissionsVisibilityCommand = new RelayCommand(ExecuteTogglePermissionsVisibility);

			PossibleAccessControlTypes = new List<AccessControlEntryType>()
			{
				AccessControlEntryType.Allow,
				AccessControlEntryType.Deny,
			};

			PossibleAccessControlInheritanceAppliesToTypes = new List<string>()
			{
				"This folder only",
				"This folder, subfolders and files",
				"This folder and subfolders",
				"This folder and files",
				"Subfolders and files only",
				"Subfolders only",
				"File only",
			};

		}

		private void ExecuteTogglePermissionsVisibility()
		{
			ShowAdvancedPermissions = !ShowAdvancedPermissions;
			PermissionsVisibilityToggleLinkButtonContent =
				ShowAdvancedPermissions
					? "Show basic permissions"
					: "Show advanced permissions";
		}

		private void UpdateAccessControl(AccessMaskFlags mask, bool value)
		{
		}
	}
}
