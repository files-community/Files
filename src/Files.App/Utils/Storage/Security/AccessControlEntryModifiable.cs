// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Utils.Storage.Security
{
	public class AccessControlEntryModifiable : ObservableObject, IAccessControlEntry
	{
		/// <inheritdoc/>
		public bool IsFolder { get; private set; }

		/// <inheritdoc/>
		public Principal Principal { get; private set; }

		/// <inheritdoc/>
		public AccessControlEntryType AccessControlType { get; private set; }

		/// <inheritdoc/>
		public bool IsInherited { get; private set; }

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

		public ICommand TogglePermissionsVisibilityCommand;

		public AccessControlEntryModifiable(IAccessControlEntry item)
		{
			IsFolder = item.IsFolder;
			Principal = item.Principal;
			SelectedAccessControlType = item.AccessControlType;
			IsInherited = item.IsInherited;

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
	}
}
