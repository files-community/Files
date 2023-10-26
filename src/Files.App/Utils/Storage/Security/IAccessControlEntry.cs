// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Represents contract of access control entry (ACE).
	/// </summary>
	public interface IAccessControlEntry
	{
		/// <summary>
		/// Gets the path that contains this entry.
		/// </summary>
		public string Path { get; }

		/// <summary>
		/// Gets the value that indicated whether the path indicates folder or not.
		/// </summary>
		public bool IsFolder { get; }

		/// <summary>
		/// Gets the owner in the security descriptor (SD).
		/// Can be <see cref="null"/> if the security descriptor has no owner SID.
		/// </summary>
		public Principal Principal { get; }

		/// <summary>
		/// Gets the access control type of this entry.
		/// </summary>
		public AccessControlEntryType AccessControlType { get; }

		/// <summary>
		/// Gets the value that indicates whether the ACE is inherited or not.
		/// </summary>
		public bool IsInherited { get; }

		/// <summary>
		/// Gets the flags of access controls the principal has.
		/// </summary>
		public AccessMaskFlags AccessMaskFlags { get; }

		/// <summary>
		/// Gets the flags which indicates how ACE will be inherited.
		/// </summary>
		public AccessControlEntryFlags AccessControlEntryFlags { get; }
	}
}
