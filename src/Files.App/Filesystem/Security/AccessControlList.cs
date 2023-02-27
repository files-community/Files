using Files.App.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an ACL.
	/// </summary>
	public class AccessControlList
	{
		public AccessControlList()
		{
			AccessControlEntryPrimitiveMappingList = new();
		}

		#region Properties
		public string? Path { get; set; }

		public bool IsFolder { get; set; }

		public bool IsAccessControlListProtected { get; set; }

		public bool CanReadAccessControl { get; set; }

		public string? OwnerSID { get; set; }

		public string? CurrentUserSID { get; set; }

		public List<AccessControlEntryPrimitiveMapping> AccessControlEntryPrimitiveMappingList { get; set; }
		#endregion

		#region Methods
		public bool SetAccessControl()
		{
			if (GetAccessControl(Path, IsFolder, out var acs))
			{
				try
				{
					var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));

					foreach (var existingRule in accessRules.Cast<FileSystemAccessRule>().Where(x => !x.IsInherited))
					{
						acs.RemoveAccessRule(existingRule);
					}

					foreach (var rule in AccessControlEntryPrimitiveMappingList.Where(x => !x.IsInherited))
					{
						acs.AddAccessRule(rule.ToFileSystemAccessRule());
					}

					if (IsFolder)
					{
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(Path), (DirectorySecurity)acs);
					}
					else
					{
						FileSystemAclExtensions.SetAccessControl(new FileInfo(Path), (FileSecurity)acs);
					}

					return true;
				}
				catch (UnauthorizedAccessException)
				{
					// User does not have rights to set access rules
					return false;
				}
				catch (Exception)
				{
					return false;
				}
			}

			return false;
		}

		public bool SetAccessControlProtection(bool isProtected, bool preserveInheritance)
		{
			if (GetAccessControl(Path, IsFolder, out var acs))
			{
				try
				{
					acs.SetAccessRuleProtection(isProtected, preserveInheritance);

					if (IsFolder)
					{
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(Path), (DirectorySecurity)acs);
					}
					else
					{
						FileSystemAclExtensions.SetAccessControl(new FileInfo(Path), (FileSecurity)acs);
					}

					return true;
				}
				catch (UnauthorizedAccessException)
				{
					// User does not have permission to set access control
					return false;
				}
				catch (Exception)
				{
					return false;
				}
			}

			return false;
		}

		public bool SetOwner(string ownerSid)
		{
			if (GetAccessControl(Path, IsFolder, out var acs))
			{
				try
				{
					acs.SetOwner(new SecurityIdentifier(ownerSid));

					if (IsFolder)
					{
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(Path), (DirectorySecurity)acs);
					}
					else
					{
						FileSystemAclExtensions.SetAccessControl(new FileInfo(Path), (FileSecurity)acs);
					}

					return true;
				}
				catch (UnauthorizedAccessException)
				{
					// User does not have permission to set the owner
					return false;
				}
				catch (Exception)
				{
					return false;
				}
			}

			// Set through powershell (admin)
			return Win32API.RunPowershellCommand($"-command \"try {{ $path = '{Path}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{ownerSid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"", true);
		}

		public FileSystemRights GetEffectiveRights()
		{
			using var user = WindowsIdentity.GetCurrent();
			var userSids = new List<string> { user.User.Value };
			userSids.AddRange(user.Groups.Select(x => x.Value));

			FileSystemRights inheritedDenyRights = 0, denyRights = 0;
			FileSystemRights inheritedAllowRights = 0, allowRights = 0;

			foreach (var Rule in AccessControlEntryPrimitiveMappingList.Where(x => userSids.Contains(x.PrincipalSid)))
			{
				if (Rule.AccessControlType == System.Security.AccessControl.AccessControlType.Deny)
				{
					if (Rule.IsInherited)
					{
						inheritedDenyRights |= Rule.FileSystemRights;
					}
					else
					{
						denyRights |= Rule.FileSystemRights;
					}
				}
				else if (Rule.AccessControlType == System.Security.AccessControl.AccessControlType.Allow)
				{
					if (Rule.IsInherited)
					{
						inheritedAllowRights |= Rule.FileSystemRights;
					}
					else
					{
						allowRights |= Rule.FileSystemRights;
					}
				}
			}

			return (inheritedAllowRights & ~inheritedDenyRights) | (allowRights & ~denyRights);
		}

		public static AccessControlList FromPath(string path, bool isFolder)
		{
			var filePermissions = new AccessControlList()
			{
				Path = path,
				IsFolder = isFolder
			};

			var acsResult = GetAccessControl(path, isFolder, out var acs);
			if (acsResult)
			{
				var rules = new List<AccessControlEntryPrimitiveMapping>();

				var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));
				foreach (var accessRule in accessRules)
				{
					rules.Add(AccessControlEntryPrimitiveMapping.FromFileSystemAccessRule((FileSystemAccessRule)accessRule));
				}

				filePermissions.AccessControlEntryPrimitiveMappingList.AddRange(rules);
				filePermissions.OwnerSID = acs.GetOwner(typeof(SecurityIdentifier)).Value;
				filePermissions.IsAccessControlListProtected = acs.AreAccessRulesProtected;
			}

			filePermissions.CanReadAccessControl = acsResult;

			return filePermissions;
		}

		private static bool GetAccessControl(string path, bool isFolder, out FileSystemSecurity fss)
		{
			try
			{
				if (isFolder && Directory.Exists(path))
				{
					fss = FileSystemAclExtensions.GetAccessControl(new DirectoryInfo(path));

					return true;
				}
				else if (File.Exists(path))
				{
					fss = FileSystemAclExtensions.GetAccessControl(new FileInfo(path));

					return true;
				}
				else
				{
					// File or folder does not exists
					fss = null;

					return false;
				}
			}
			catch (UnauthorizedAccessException)
			{
				// User does not have rights to read access rules
				fss = null;

				return false;
			}
			catch (Exception)
			{
				fss = null;

				return false;
			}
		}
		#endregion
	}
}
