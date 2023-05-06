// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an ACL.
	/// </summary>
	public class AccessControlList
	{
		/// <summary>
		/// File owner information
		/// </summary>
		public Principal Owner { get; set; }

		/// <summary>
		/// Whether the DACL is protected
		/// </summary>
		public bool IsProtected { get; set; }

		/// <summary>
		/// Whether the DACL is valid one
		/// </summary>
		public bool IsValid { get; set; }

		/// <summary>
		/// File path which have this access control list
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// Whether the path indicates folder or not
		/// </summary>
		public bool IsFolder { get; set; }

		/// <summary>
		/// ACE list
		/// </summary>
		public ObservableCollection<AccessControlEntry> AccessControlEntries { get; set; }

		public AccessControlList()
		{
		}
	}
}
