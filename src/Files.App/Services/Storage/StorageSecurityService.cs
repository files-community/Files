// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.System.Memory;
using SystemSecurity = System.Security.AccessControl;

namespace Files.App.Services
{
	/// <inheritdoc cref="IStorageSecurityService"/>
	public class StorageSecurityService : IStorageSecurityService
	{
		/// <inheritdoc/>
		public unsafe string GetOwner(string path)
		{
			var result = PInvoke.GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION,
				out var pSidOwner,
				out _,
				out _,
				out _,
				out var pSecurityDescriptor);
			if (result is not WIN32_ERROR.ERROR_SUCCESS)
				return string.Empty;

			try
			{
				if (!PInvoke.ConvertSidToStringSid(pSidOwner, out var sid))
					return string.Empty;

				try
				{
					return sid.ToString();
				}
				finally
				{
					Marshal.FreeHGlobal((nint)sid.Value);
				}
			}
			finally
			{
				Marshal.FreeHGlobal((nint)pSecurityDescriptor.Value);
			}
		}

		/// <inheritdoc/>
		public unsafe bool SetOwner(string path, string sid)
		{
			PSID pSid = default;

			// Get SID
			fixed (char* cSid = sid)
			{
				if (!PInvoke.ConvertStringSidToSid(new PCWSTR(cSid), &pSid))
					return false;
			}

			try
			{
				WIN32_ERROR result;
				fixed (char* cPath = path)
				{
					// Change owner
					result = PInvoke.SetNamedSecurityInfo(
						new PWSTR(cPath),
						SE_OBJECT_TYPE.SE_FILE_OBJECT,
						OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION,
						pSid,
						new PSID((void*)0));
				}

				// Run PowerShell as Admin
				if (result is not WIN32_ERROR.ERROR_SUCCESS)
				{
					return Win32Helper.RunPowershellCommand(
						$"-command \"try {{ $path = {Win32Helper.ToPowerShellStringLiteral(path)}; $ID = new-object System.Security.Principal.SecurityIdentifier({Win32Helper.ToPowerShellStringLiteral(sid)}); $acl = get-acl -LiteralPath $path; $acl.SetOwner($ID); set-acl -LiteralPath $path -aclObject $acl }} catch {{ exit 1; }}\"",
						PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
				}

				return true;
			}
			finally
			{
				Marshal.FreeHGlobal((nint)pSid.Value);
			}
		}

		/// <inheritdoc/>
		public unsafe WIN32_ERROR GetAcl(string path, bool isFolder, out AccessControlList acl)
		{
			acl = new();

			// Get DACL
			var result = PInvoke.GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out ACL* pDACL,
				out _,
				out var pSecurityDescriptor);

			if (result is not WIN32_ERROR.ERROR_SUCCESS)
				return result;

			try
			{
				if (pDACL == null)
					return WIN32_ERROR.ERROR_SUCCESS;

				ACL_SIZE_INFORMATION aclSizeInfo = default;

				// Get ACL size info
				bool bResult = PInvoke.GetAclInformation(
					pDACL,
					&aclSizeInfo,
					(uint)Marshal.SizeOf<ACL_SIZE_INFORMATION>(),
					ACL_INFORMATION_CLASS.AclSizeInformation);

				if (!bResult)
					return (WIN32_ERROR)Marshal.GetLastPInvokeError();

				// Get owner
				var szOwnerSid = GetOwner(path);
				var principal = new AccessControlPrincipal(szOwnerSid);

				var isValidAcl = PInvoke.IsValidAcl(pDACL);
				List<AccessControlEntry> aces = [];

				// Get ACEs
				for (uint i = 0; i < aclSizeInfo.AceCount; i++)
				{
					bResult = PInvoke.GetAce(*pDACL, i, out var pAce);
					if (!bResult)
						return (WIN32_ERROR)Marshal.GetLastPInvokeError();

					if (pAce is null)
						continue;

					var ace = Marshal.PtrToStructure<ACCESS_ALLOWED_ACE>((nint)pAce);
					var offset = Marshal.SizeOf<ACE_HEADER>() + sizeof(uint);
					nint pAcePtr = new((long)pAce + offset);
					if (!PInvoke.ConvertSidToStringSid((PSID)pAcePtr, out var pszSid))
						return (WIN32_ERROR)Marshal.GetLastPInvokeError();

					try
					{
						AccessControlEntryType type;
						AccessControlEntryFlags inheritanceFlags = AccessControlEntryFlags.None;
						AccessMaskFlags accessMaskFlags = (AccessMaskFlags)ace.Mask;

						var header = ace.Header;
						type = (SystemSecurity.AceType)header.AceType switch
						{
							SystemSecurity.AceType.AccessAllowed => AccessControlEntryType.Allow,
							_ => AccessControlEntryType.Deny
						};

						var flags = (SystemSecurity.AceFlags)header.AceFlags;
						bool isInherited = flags.HasFlag(SystemSecurity.AceFlags.Inherited);

						if (flags.HasFlag(SystemSecurity.AceFlags.ContainerInherit))
							inheritanceFlags |= AccessControlEntryFlags.ContainerInherit;
						if (flags.HasFlag(SystemSecurity.AceFlags.ObjectInherit))
							inheritanceFlags |= AccessControlEntryFlags.ObjectInherit;
						if (flags.HasFlag(SystemSecurity.AceFlags.NoPropagateInherit))
							inheritanceFlags |= AccessControlEntryFlags.NoPropagateInherit;
						if (flags.HasFlag(SystemSecurity.AceFlags.InheritOnly))
							inheritanceFlags |= AccessControlEntryFlags.InheritOnly;

						aces.Add(new(isFolder, pszSid.ToString(), type, accessMaskFlags, isInherited, inheritanceFlags));
					}
					finally
					{
						Marshal.FreeHGlobal((nint)pszSid.Value);
					}
				}

				// Initialize with proper data
				acl = new AccessControlList(path, isFolder, principal, isValidAcl);

				// Set access control entries
				foreach (var ace in aces)
					acl.AccessControlEntries.Add(ace);

				return WIN32_ERROR.ERROR_SUCCESS;
			}
			finally
			{
				Marshal.FreeHGlobal((nint)pSecurityDescriptor.Value);
			}
		}

		/// <inheritdoc/>
		public unsafe WIN32_ERROR AddAce(string szPath, bool isFolder, string szSid)
		{
			// Get DACL for the specified object
			var result = PInvoke.GetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out ACL* pDACL,
				out _,
				out var pSecurityDescriptor);

			if (result is not WIN32_ERROR.ERROR_SUCCESS)
				return result;

			try
			{
				if (pDACL == null)
					return WIN32_ERROR.ERROR_INVALID_ACL;

				ACL_SIZE_INFORMATION aclSizeInfo = default;
				bool bResult = PInvoke.GetAclInformation(
					pDACL,
					&aclSizeInfo,
					(uint)Marshal.SizeOf<ACL_SIZE_INFORMATION>(),
					ACL_INFORMATION_CLASS.AclSizeInformation);
				if (!bResult)
					return (WIN32_ERROR)Marshal.GetLastPInvokeError();

				PSID pSid = default;
				fixed (char* cSid = szSid)
				{
					if (!PInvoke.ConvertStringSidToSid(new PCWSTR(cSid), &pSid))
						return (WIN32_ERROR)Marshal.GetLastPInvokeError();
				}

				try
				{
					var sidLength = (uint)new SecurityIdentifier(szSid).BinaryLength;
					var cbNewDACL = checked(
						aclSizeInfo.AclBytesInUse +
						(uint)Marshal.SizeOf<ACCESS_ALLOWED_ACE>() +
						sidLength -
						(uint)sizeof(uint));
					var pNewDACL = (ACL*)PInvoke.LocalAlloc(LOCAL_ALLOC_FLAGS.LPTR, (nuint)cbNewDACL);
					if (pNewDACL == null)
						return (WIN32_ERROR)Marshal.GetLastPInvokeError();

					try
					{
						if (!PInvoke.InitializeAcl(pNewDACL, cbNewDACL, ACE_REVISION.ACL_REVISION))
							return (WIN32_ERROR)Marshal.GetLastPInvokeError();

						// Copy ACEs from the old DACL
						for (uint dwAceIndex = 0; dwAceIndex < aclSizeInfo.AceCount; dwAceIndex++)
						{
							ACCESS_ALLOWED_ACE* pTempAce = default;
							if (!PInvoke.GetAce(pDACL, dwAceIndex, (void**)&pTempAce))
								return (WIN32_ERROR)Marshal.GetLastPInvokeError();
							if (!PInvoke.AddAce(pNewDACL, ACE_REVISION.ACL_REVISION, uint.MaxValue, pTempAce, pTempAce->Header.AceSize))
								return (WIN32_ERROR)Marshal.GetLastPInvokeError();
						}

						bResult = PInvoke.AddAccessAllowedAceEx(
							pNewDACL,
							ACE_REVISION.ACL_REVISION,
							isFolder ? ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE : ACE_FLAGS.NO_INHERITANCE,
							0x20000000 | 0x80000000 /* GENERIC_EXECUTE and GENERIC_READ */,
							pSid);
						if (!bResult)
							return (WIN32_ERROR)Marshal.GetLastPInvokeError();

						fixed (char* cPath = szPath)
						{
							return PInvoke.SetNamedSecurityInfo(
								new PWSTR(cPath),
								SE_OBJECT_TYPE.SE_FILE_OBJECT,
								OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
								new PSID((void*)0),
								new PSID((void*)0),
								pNewDACL);
						}
					}
					finally
					{
						Marshal.FreeHGlobal((nint)pNewDACL);
					}
				}
				finally
				{
					Marshal.FreeHGlobal((nint)pSid.Value);
				}
			}
			finally
			{
				Marshal.FreeHGlobal((nint)pSecurityDescriptor.Value);
			}
		}

		/// <inheritdoc/>
		public unsafe WIN32_ERROR DeleteAce(string szPath, uint dwAceIndex)
		{
			// Get DACL for the specified object
			var result = PInvoke.GetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				out ACL* pDACL,
				out _,
				out var pSecurityDescriptor);

			if (result is not WIN32_ERROR.ERROR_SUCCESS)
				return result;

			try
			{
				if (pDACL == null)
					return WIN32_ERROR.ERROR_INVALID_ACL;

				// Remove an ACE
				if (!PInvoke.DeleteAce(pDACL, dwAceIndex))
					return (WIN32_ERROR)Marshal.GetLastPInvokeError();

				fixed (char* cPath = szPath)
				{
					return PInvoke.SetNamedSecurityInfo(
						new PWSTR(cPath),
						SE_OBJECT_TYPE.SE_FILE_OBJECT,
						OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
						new PSID((void*)0),
						new PSID((void*)0),
						pDACL);
				}
			}
			finally
			{
				Marshal.FreeHGlobal((nint)pSecurityDescriptor.Value);
			}
		}
	}
}
