// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.Storage.FileSystem;
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
					var script = SetOwnerScript
						.Replace("__PATH__", Win32Helper.ToPowerShellStringLiteral(path))
						.Replace("__SID__", Win32Helper.ToPowerShellStringLiteral(sid));

					var encodedScript = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));

					Win32Helper.RunPowershellCommand(
						$"-NoProfile -EncodedCommand {encodedScript}",
						PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);

					return string.Equals(GetOwner(path), sid, StringComparison.OrdinalIgnoreCase);
				}

				return true;
			}
			finally
			{
				Marshal.FreeHGlobal((nint)pSid.Value);
			}
		}

		private const string SetOwnerScript = """
			Add-Type -TypeDefinition @'
			using System;
			using System.Runtime.InteropServices;

			public static class FilesSetOwner
			{
				[StructLayout(LayoutKind.Sequential)]
				public struct LUID { public uint LowPart; public int HighPart; }

				[StructLayout(LayoutKind.Sequential)]
				public struct LUID_AND_ATTRIBUTES { public LUID Luid; public uint Attributes; }

				[StructLayout(LayoutKind.Sequential)]
				public struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID_AND_ATTRIBUTES Privilege; }

				private const int SE_FILE_OBJECT = 1;
				private const uint OWNER_SECURITY_INFORMATION = 0x00000001;

				[DllImport("kernel32.dll")]
				private static extern IntPtr GetCurrentProcess();

				[DllImport("kernel32.dll")]
				private static extern IntPtr LocalFree(IntPtr handle);

				[DllImport("kernel32.dll")]
				private static extern bool CloseHandle(IntPtr handle);

				[DllImport("advapi32.dll", SetLastError = true)]
				private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

				[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
				private static extern bool LookupPrivilegeValue(string host, string name, out LUID luid);

				[DllImport("advapi32.dll", SetLastError = true)]
				private static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TOKEN_PRIVILEGES state, uint length, IntPtr previous, IntPtr returnLength);

				[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
				private static extern bool ConvertStringSidToSid(string sid, out IntPtr pSid);

				[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
				private static extern uint SetNamedSecurityInfo(string objectName, int objectType, uint securityInformation, IntPtr owner, IntPtr group, IntPtr dacl, IntPtr sacl);

				public static uint Run(string path, string sid)
				{
					IntPtr token;
					if (OpenProcessToken(GetCurrentProcess(), 0x0020 | 0x0008, out token))
					{
						string[] names = new string[] { "SeTakeOwnershipPrivilege", "SeRestorePrivilege" };

						foreach (string name in names)
						{
							LUID luid;
							if (!LookupPrivilegeValue(null, name, out luid))
								continue;

							TOKEN_PRIVILEGES privileges = new TOKEN_PRIVILEGES();
							privileges.PrivilegeCount = 1;
							privileges.Privilege.Luid = luid;
							privileges.Privilege.Attributes = 0x0002;

							AdjustTokenPrivileges(token, false, ref privileges, (uint)Marshal.SizeOf(privileges), IntPtr.Zero, IntPtr.Zero);
						}

						CloseHandle(token);
					}

					IntPtr pSid;
					if (!ConvertStringSidToSid(sid, out pSid))
						return 87;

					uint result = SetNamedSecurityInfo(path, SE_FILE_OBJECT, OWNER_SECURITY_INFORMATION, pSid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

					LocalFree(pSid);

					return result;
				}
			}
			'@

			exit [FilesSetOwner]::Run(__PATH__, __SID__)
			""";

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
		public bool CanWriteAcl(string path, bool isFolder)
		{
			using var handle = PInvoke.CreateFile(
				path,
				(uint)FILE_ACCESS_RIGHTS.WRITE_DAC,
				FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
				null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				isFolder ? FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS : FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
				null);

			if (!handle.IsInvalid)
				return true;

			// Only an explicit denial proves the write would fail. Any other failure is
			// inconclusive, so let the attempt through and report the real error instead
			return (WIN32_ERROR)Marshal.GetLastPInvokeError() != WIN32_ERROR.ERROR_ACCESS_DENIED;
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
