// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an access control list (ACL).
	/// </summary>
	public class AccessControlList
	{
		/// <summary>
		/// Object path.
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Whether the path indicates folder or not.
		/// </summary>
		public bool IsFolder { get; private set; }

		/// <summary>
		/// The owner in the security descriptor (SD).
		/// NULL if the security descriptor has no owner SID.
		/// </summary>
		public Principal? Owner { get; private set; }

		/// <summary>
		/// Validates an access control list (ACL).
		/// </summary>
		public bool IsValid { get; private set; }

		/// <summary>
		/// Whether the viewer has 'Read Permissions' access control or not.
		/// If not, the user cannot view access control list (ACL).
		/// </summary>
		public bool ViewerHasReadPermissionAccessControl { get; private set; }

		/// <summary>
		/// Access control entry (ACE) list
		/// </summary>
		public ObservableCollection<AccessControlEntry> AccessControlEntries { get; private set; }

		public AccessControlList(string path, bool isFolder, Principal? owner, bool isValid)
		{
			Path = path;
			IsFolder = isFolder;
			Owner = owner;
			IsValid = isValid;
			AccessControlEntries = new();
		}

		public AccessControlList(bool canRead)
		{
			Path = string.Empty;
			ViewerHasReadPermissionAccessControl = canRead;
			AccessControlEntries = new();
		}
	}
}
