// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Security;
using Files.App.Shell;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Secur32;
using FilesSecurity = Files.App.Filesystem.Security;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents a helper for file security information.
	/// </summary>
	public static class FileSecurityHelpers
	{
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

		/// <summary>The GetAclInformation function retrieves information about an access control list (ACL).</summary>
		/// <returns>
		/// If the function succeeds, the function returns AccessControlList. If the function fails, it returns null. To get extended error information, call GetLastError.
		/// </returns>
		public static AccessControlList GetAccessControlList(string path, bool isFolder)
		{
			// Get DACL
			var win32Error = GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _, out _, out var pDacl, out _, out _);

			if (win32Error.Failed || pDacl == PACL.NULL)
				return new AccessControlList(false);

			// Get ACL size info
			bool bResult = GetAclInformation(pDacl, out ACL_SIZE_INFORMATION aclSize);
			if (!bResult)
				return new AccessControlList(false);

			// Get owner
			var szOwnerSid = GetOwner(path);
			var principal = Principal.FromSid(szOwnerSid);

			var isValidAcl = IsValidAcl(pDacl);

			uint chhCurrentUserName = 256 + 1;
			StringBuilder szCurrentUserName = new((int)chhCurrentUserName);

			// Get the current username
			bResult = GetUserName(szCurrentUserName, ref chhCurrentUserName);
			if (!bResult)
				return new AccessControlList(false);

			// Get sid from the current username
			var pCurrentUserSid = GetSid(szCurrentUserName.ToString());
			var szCurrentUserStringSid = ConvertSidToStringSid(pCurrentUserSid);

			List<AccessControlEntry> aces = new();

			// Get ACEs
			for (uint i = 0; i < aclSize.AceCount; i++)
			{
				bResult = GetAce(pDacl, i, out var pAce);
				if (!bResult)
					return new AccessControlList(false);

				var szSid = ConvertSidToStringSid(pAce.GetSid());

				FilesSecurity.AccessControlType type;
				FilesSecurity.AccessControlEntryFlags inheritanceFlags = FilesSecurity.AccessControlEntryFlags.None;
				AccessMaskFlags accessMaskFlags = (AccessMaskFlags)pAce.GetMask();

				// Get if the viewer has 'Read Permissions' access control
				if (szCurrentUserStringSid == szSid &&
					accessMaskFlags.HasFlag(AccessMaskFlags.ReadPermissions))
					return new AccessControlList(false);

				var header = pAce.GetHeader();
				type = header.AceType switch
				{
					AceType.AccessAllowed => FilesSecurity.AccessControlType.Allow,
					_ => FilesSecurity.AccessControlType.Deny
				};

				bool isInherited = header.AceFlags.HasFlag(AceFlags.InheritanceFlags);

				if (header.AceFlags.HasFlag(AceFlags.ContainerInherit))
					inheritanceFlags |= FilesSecurity.AccessControlEntryFlags.ContainerInherit;
				if (header.AceFlags.HasFlag(AceFlags.ObjectInherit))
					inheritanceFlags |= FilesSecurity.AccessControlEntryFlags.ObjectInherit;
				if (header.AceFlags.HasFlag(AceFlags.NoPropagateInherit))
					inheritanceFlags |= FilesSecurity.AccessControlEntryFlags.NoPropagateInherit;
				if (header.AceFlags.HasFlag(AceFlags.InheritOnly))
					inheritanceFlags |= FilesSecurity.AccessControlEntryFlags.InheritOnly;

				// Initialize an ACE
				aces.Add(new(isFolder, szSid, type, accessMaskFlags, isInherited, inheritanceFlags));
			}

			// Initialize
			var acl = new AccessControlList(path, isFolder, principal, isValidAcl);

			// Set access control entries
			foreach (var ace in aces)
				acl.AccessControlEntries.Add(ace);

			return acl;
		}

		public static AccessControlEntry InitializeDefaultAccessControlEntry(bool isFolder, string ownerSid)
		{
			return new(
				isFolder,
				ownerSid,
				FilesSecurity.AccessControlType.Allow,
				FilesSecurity.AccessMaskFlags.ReadAndExecute,
				false,
				isFolder
					? FilesSecurity.AccessControlEntryFlags.ContainerInherit | FilesSecurity.AccessControlEntryFlags.ObjectInherit
					: FilesSecurity.AccessControlEntryFlags.None);
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

		private static PSID GetSid(string accountName)
		{
			int sidSize = 0;
			int nameSize = 0;

			LookupAccountName(null, accountName, SafePSID.Null, ref sidSize, null, ref nameSize, out _);
			var domainName = new StringBuilder(nameSize);

			SizeT size = new((uint)sidSize);
			var sid = new SafePSID(size);

			LookupAccountName(string.Empty, accountName, sid, ref sidSize, domainName, ref nameSize, out _);

			return sid;
		}
	}
}
