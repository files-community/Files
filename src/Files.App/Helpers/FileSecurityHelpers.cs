// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Security;
using Files.App.Shell;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using SystemSecurity = System.Security.AccessControl;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents a helper for file security information.
	/// </summary>
	public static class FileSecurityHelpers
	{
		/// <summary>
		/// Get file owner.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string GetOwner(string path)
		{
			GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION,
				out var pSidOwner,
				out _,
				out _,
				out _,
				out _);

			var szSid = ConvertSidToStringSid(pSidOwner);

			return szSid;
		}

		/// <summary>
		/// Set file owner
		/// </summary>
		/// <param name="path"></param>
		/// <param name="sid"></param>
		/// <returns></returns>
		public static bool SetOwner(string path, string sid)
		{
			SECURITY_INFORMATION secInfo = SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION;

			// Get PSID object from string sid
			var pSid = ConvertStringSidToSid(sid);

			// Change owner
			var result = SetNamedSecurityInfo(path, SE_OBJECT_TYPE.SE_FILE_OBJECT, secInfo, pSid);

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
			var win32Error = GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _, out _, out var pDacl, out _, out _);

			if (win32Error.Failed || pDacl == PACL.NULL)
				return win32Error;

			// Get ACL size info
			bool bResult = GetAclInformation(pDacl, out ACL_SIZE_INFORMATION aclSize);
			if (!bResult)
				return Kernel32.GetLastError();

			// Get owner
			var szOwnerSid = GetOwner(path);
			var principal = Principal.FromSid(szOwnerSid);

			var isValidAcl = IsValidAcl(pDacl);

			List<AccessControlEntry> aces = new();

			// Get ACEs
			for (uint i = 0; i < aclSize.AceCount; i++)
			{
				bResult = GetAce(pDacl, i, out var pAce);
				if (!bResult)
					return Kernel32.GetLastError();

				var szSid = ConvertSidToStringSid(pAce.GetSid());

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
			var result = GetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out PACL pDACL,
				out _,
				out _);

			if (result.Failed)
				return result;

			// Initialize default trustee
			var explicitAccess = new EXPLICIT_ACCESS
			{
				grfAccessMode = ACCESS_MODE.GRANT_ACCESS,
				grfAccessPermissions = ACCESS_MASK.GENERIC_READ | ACCESS_MASK.GENERIC_EXECUTE,
				grfInheritance = INHERIT_FLAGS.NO_INHERITANCE,
				Trustee = new TRUSTEE(new SafePSID(szSid)),
			};

			// Add an new ACE and get a new ACL
			result = SetEntriesInAcl(1, new[] { explicitAccess }, pDACL, out var pNewDACL);

			if (result.Failed)
				return result;

			// Set the new ACL
			result = SetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
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
			var result = GetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out var pDACL,
				out _,
				out _);

			if (result.Failed)
				return result;

			// Remove an ACE
			bool bResult = DeleteAce(pDACL, dwAceIndex);

			if (!bResult)
				return Kernel32.GetLastError();

			// Set the new ACL
			result = SetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				ppDacl: pDACL);

			if (result.Failed)
				return result;

			return result;
		}
	}
}
