// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Security;
using Files.App.Shell;
using System.IO;
using System.Security.AccessControl;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using FilesSecurity = Files.App.Filesystem.Security;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper to get and set file security information.
	/// </summary>
	public static class FileSecurityHelpers
	{
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
				return Win32API.RunPowershellCommand($"-command \"try {{ $path = '{path}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{sid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"", true);
			}

			return true;
		}

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

		public static bool GetAccessControlProtection(string path, bool isFolder)
		{
			FileSystemSecurity fileSystemSecurity;

			if (isFolder && Directory.Exists(path))
			{
				fileSystemSecurity = FileSystemAclExtensions.GetAccessControl(new DirectoryInfo(path));
				return fileSystemSecurity.AreAccessRulesProtected;
			}
			else if (File.Exists(path))
			{
				fileSystemSecurity = FileSystemAclExtensions.GetAccessControl(new FileInfo(path));
				return fileSystemSecurity.AreAccessRulesProtected;
			}
			else
			{
				return false;
			}
		}

		public static bool SetAccessControlProtection(string path, bool isFolder, bool isProtected, bool preserveInheritance)
		{
			FileSystemSecurity fileSystemSecurity;

			if (isFolder && Directory.Exists(path))
			{
				fileSystemSecurity = FileSystemAclExtensions.GetAccessControl(new DirectoryInfo(path));
				fileSystemSecurity.SetAccessRuleProtection(isProtected, preserveInheritance);
				FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(path), (DirectorySecurity)fileSystemSecurity);

				return true;
			}
			else if (File.Exists(path))
			{
				fileSystemSecurity = FileSystemAclExtensions.GetAccessControl(new FileInfo(path));
				fileSystemSecurity.SetAccessRuleProtection(isProtected, preserveInheritance);
				FileSystemAclExtensions.SetAccessControl(new FileInfo(path), (FileSecurity)fileSystemSecurity);

				return true;
			}
			else
			{
				return false;
			}
		}

		public static AccessControlList GetAccessControlList(string path, bool isFolder)
		{
			// Get DACL
			GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out var pDacl,
				out _,
				out _);

			// Get ACL size info
			GetAclInformation(pDacl, out ACL_SIZE_INFORMATION aclSize);

			// Get owner
			var szOwnerSid = GetOwner(path);

			// Initialize
			var acl = new AccessControlList()
			{
				Owner = Principal.FromSid(szOwnerSid),
				IsProtected = GetAccessControlProtection(path, isFolder),
				IsValid = true,
				AccessControlEntries = new(),
				Path = path,
				IsFolder = isFolder
			};

			// Get ACEs
			for (uint i = 0; i < aclSize.AceCount; i++)
			{
				GetAce(pDacl, i, out var pAce);

				var szSid = ConvertSidToStringSid(pAce.GetSid());

				var header = pAce.GetHeader();

				FilesSecurity.AccessControlType type;
				FilesSecurity.InheritanceFlags inheritanceFlags = FilesSecurity.InheritanceFlags.None;
				FilesSecurity.PropagationFlags propagationFlags = FilesSecurity.PropagationFlags.None;
				AccessMaskFlags accessMaskFlags = (AccessMaskFlags)pAce.GetMask();

				type = header.AceType switch
				{
					AceType.AccessAllowed => FilesSecurity.AccessControlType.Allow,
					_ => FilesSecurity.AccessControlType.Deny
				};

				bool isInherited = header.AceFlags.HasFlag(AceFlags.InheritanceFlags);

				if (header.AceFlags.HasFlag(AceFlags.ContainerInherit))
					inheritanceFlags |= FilesSecurity.InheritanceFlags.ContainerInherit;
				if (header.AceFlags.HasFlag(AceFlags.ObjectInherit))
					inheritanceFlags |= FilesSecurity.InheritanceFlags.ObjectInherit;
				if (header.AceFlags.HasFlag(AceFlags.NoPropagateInherit))
					propagationFlags |= FilesSecurity.PropagationFlags.NoPropagateInherit;
				if (header.AceFlags.HasFlag(AceFlags.InheritOnly))
					propagationFlags |= FilesSecurity.PropagationFlags.InheritOnly;

				// Initialize an ACE
				acl.AccessControlEntries.Add(new(isFolder, szSid, type, accessMaskFlags, isInherited, inheritanceFlags, propagationFlags));
			}

			return acl;
		}

		public static bool SetAccessControlList(AccessControlList acl)
		{
			return false;
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
					? FilesSecurity.InheritanceFlags.ContainerInherit | FilesSecurity.InheritanceFlags.ObjectInherit
					: FilesSecurity.InheritanceFlags.None,
				FilesSecurity.PropagationFlags.None);
		}

		public static bool AddAccessControlEntry(string path, AccessControlEntry entry)
		{
			// Get DACL
			GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out var pDacl,
				out _,
				out _);

			// Get ACL size info
			GetAclInformation(pDacl, out ACL_SIZE_INFORMATION aclSize);

			uint revision = GetAclInformation(pDacl, out ACL_REVISION_INFORMATION aclRevision) ? aclRevision.AclRevision : 0U;

			// Get ACEs
			for (uint i = 0; i < aclSize.AceCount; i++)
			{
				//GetAce(pDacl, i, out var pAce);

				//AddAce(pDacl, revision, 0, (IntPtr)pAce, ((PACL)pDacl).Length() - (uint)Marshal.SizeOf(typeof(ACL)));
			}

			return false;
		}
	}
}
