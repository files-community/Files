// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Security;
using Files.App.Shell;
using Vanara.PInvoke;
using SystemSecurity = System.Security.AccessControl;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents a helper for file security information.
	/// </summary>
	public static class FileSecurityHelpers
	{
		/// <summary>
		/// Get the owner of the object specified by the path.
		/// </summary>
		/// <param name="path">The file full path</param>
		/// <returns></returns>
		public static string GetOwner(string path)
		{
			AdvApi32.GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION,
				out var pSidOwner,
				out _,
				out _,
				out _,
				out _);

			var szSid = AdvApi32.ConvertSidToStringSid(pSidOwner);

			return szSid;
		}

		/// <summary>
		/// Set the owner of the object specified by the path.
		/// </summary>
		/// <param name="path">The file full path</param>
		/// <param name="sid">The owner security identifier (SID)</param>
		/// <returns></returns>
		public static bool SetOwner(string path, string sid)
		{
			SECURITY_INFORMATION secInfo = SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION;

			// Get PSID object from string sid
			var pSid = AdvApi32.ConvertStringSidToSid(sid);

			// Change owner
			var result = AdvApi32.SetNamedSecurityInfo(path, AdvApi32.SE_OBJECT_TYPE.SE_FILE_OBJECT, secInfo, pSid);

			pSid.Dispose();

			// Run PowerShell as Admin
			if (result.Failed)
			{
				return Win32API.RunPowershellCommand(
					$"-command \"try {{ $path = '{path}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{sid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"",
					true);
			}

			return true;
		}

		/// <summary>
		/// Get information about an access control list (ACL).
		/// </summary>
		/// <param name="path"></param>
		/// <param name="isFolder"></param>
		/// <returns>If the function succeeds, an instance of AccessControlList; otherwise, null. To get extended error information, call GetLastError.</returns>
		public static Win32Error GetAccessControlList(string path, bool isFolder, out AccessControlList acl)
		{
			acl = new();

			// Get DACL
			var win32Error = AdvApi32.GetNamedSecurityInfo(
				path,
				AdvApi32.SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out var pDacl,
				out _,
				out _);

			if (win32Error.Failed || pDacl == PACL.NULL)
				return win32Error;

			// Get ACL size info
			bool bResult = AdvApi32.GetAclInformation(pDacl, out AdvApi32.ACL_SIZE_INFORMATION aclSize);
			if (!bResult)
				return Kernel32.GetLastError();

			// Get owner
			var szOwnerSid = GetOwner(path);
			var principal = new Principal(szOwnerSid);

			var isValidAcl = AdvApi32.IsValidAcl(pDacl);

			List<AccessControlEntry> aces = new();

			// Get ACEs
			for (uint i = 0; i < aclSize.AceCount; i++)
			{
				bResult = AdvApi32.GetAce(pDacl, i, out var pAce);
				if (!bResult)
					return Kernel32.GetLastError();

				var szSid = AdvApi32.ConvertSidToStringSid(pAce.GetSid());

				AccessControlEntryType type;
				AccessControlEntryFlags inheritanceFlags = AccessControlEntryFlags.None;
				AccessMaskFlags accessMaskFlags = (AccessMaskFlags)pAce.GetMask();

				var header = pAce.GetHeader();
				type = header.AceType switch
				{
					SystemSecurity.AceType.AccessAllowed => AccessControlEntryType.Allow,
					_ => AccessControlEntryType.Deny
				};

				bool isInherited = header.AceFlags.HasFlag(SystemSecurity.AceFlags.InheritanceFlags);

				if (header.AceFlags.HasFlag(SystemSecurity.AceFlags.ContainerInherit))
					inheritanceFlags |= AccessControlEntryFlags.ContainerInherit;
				if (header.AceFlags.HasFlag(SystemSecurity.AceFlags.ObjectInherit))
					inheritanceFlags |= AccessControlEntryFlags.ObjectInherit;
				if (header.AceFlags.HasFlag(SystemSecurity.AceFlags.NoPropagateInherit))
					inheritanceFlags |= AccessControlEntryFlags.NoPropagateInherit;
				if (header.AceFlags.HasFlag(SystemSecurity.AceFlags.InheritOnly))
					inheritanceFlags |= AccessControlEntryFlags.InheritOnly;

				// Initialize an ACE
				aces.Add(new(isFolder, szSid, type, accessMaskFlags, isInherited, inheritanceFlags));
			}

			// Initialize with proper data
			acl = new AccessControlList(path, isFolder, principal, isValidAcl);

			// Set access control entries
			foreach (var ace in aces)
				acl.AccessControlEntries.Add(ace);

			return Kernel32.GetLastError();
		}

		/// <summary>
		/// Get access control list (ACL) initialized with default data.
		/// </summary>
		/// <param name="isFolder"></param>
		/// <param name="ownerSid"></param>
		/// <returns>If the function succeeds, an instance of AccessControlList; otherwise, null.</returns>
		public static AccessControlEntry InitializeDefaultAccessControlEntry(bool isFolder, string ownerSid)
		{
			return new(
				isFolder,
				ownerSid,
				AccessControlEntryType.Allow,
				AccessMaskFlags.ReadAndExecute,
				false,
				isFolder
					? AccessControlEntryFlags.ContainerInherit | AccessControlEntryFlags.ObjectInherit
					: AccessControlEntryFlags.None);
		}

		/// <summary>
		/// Add an default Access Control Entry (ACE) to the specified object's DACL
		/// </summary>
		/// <param name="path">The object's path to add an new ACE to its DACL</param>
		/// <param name="sid">Principal's SID</param>
		/// <returns> If the function succeeds, the return value is ERROR_SUCCESS. If the function fails, the return value is a nonzero error code defined in WinError.h.</returns>
		public static Win32Error AddAccessControlEntry(string szPath, string szSid)
		{
			// Get DACL for the specified object
			var result = AdvApi32.GetNamedSecurityInfo(
				szPath,
				AdvApi32.SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out var pDACL,
				out _,
				out _);

			if (result.Failed)
				return result;

			// Initialize default trustee
			var explicitAccess = new AdvApi32.EXPLICIT_ACCESS
			{
				grfAccessMode = AdvApi32.ACCESS_MODE.GRANT_ACCESS,
				grfAccessPermissions = ACCESS_MASK.GENERIC_READ | ACCESS_MASK.GENERIC_EXECUTE,
				grfInheritance = AdvApi32.INHERIT_FLAGS.NO_INHERITANCE,
				Trustee = new AdvApi32.TRUSTEE(new AdvApi32.SafePSID(szSid)),
			};

			// Add an new ACE and get a new ACL
			result = AdvApi32.SetEntriesInAcl(1, new[] { explicitAccess }, pDACL, out var pNewDACL);

			if (result.Failed)
				return result;

			// Set the new ACL
			result = AdvApi32.SetNamedSecurityInfo(
				szPath,
				AdvApi32.SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				ppDacl: pNewDACL);

			if (result.Failed)
				return result;

			return result;
		}

		/// <summary>
		/// Add an Access Control Entry (ACE) from the specified object's DACL
		/// </summary>
		/// <param name="szPath">The object's path to remove an ACE from its DACL</param>
		/// <param name="dwAceIndex"></param>
		/// <returns></returns>
		public static Win32Error RemoveAccessControlEntry(string szPath, uint dwAceIndex)
		{
			// Get DACL for the specified object
			var result = AdvApi32.GetNamedSecurityInfo(
				szPath,
				AdvApi32.SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out var pDACL,
				out _,
				out _);

			if (result.Failed)
				return result;

			// Remove an ACE
			bool bResult = AdvApi32.DeleteAce(pDACL, dwAceIndex);

			if (!bResult)
				return Kernel32.GetLastError();

			// Set the new ACL
			result = AdvApi32.SetNamedSecurityInfo(
				szPath,
				AdvApi32.SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				ppDacl: pDACL);

			if (result.Failed)
				return result;

			return result;
		}
	}
}
