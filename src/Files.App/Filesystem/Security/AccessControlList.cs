using Files.App.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		public Principal Owner { get; private set; }

		public string? OwnerSID { get; set; }

		public bool IsAccessControlListProtected { get; set; }

		public bool CanReadAccessControl { get; set; }

		public List<AccessControlEntryPrimitiveMapping> AccessControlEntryPrimitiveMappingList { get; set; }

		public ObservableCollection<AccessControlEntryAdvanced> AccessControlEntriesAdvanced { get; set; }

		public ObservableCollection<AccessControlEntry> AccessControlEntries { get; private set; }
		#endregion

		#region Methods
		public bool SetOwner(string sid)
		{
			if (GetAccessControl(Path, IsFolder, out var acs))
			{
				try
				{
					acs.SetOwner(new SecurityIdentifier(sid));

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
				}
				catch (Exception)
				{
				}
			}

			// If previous operation was unauthorized access, try again trough elevated PowerShell
			return Win32API.RunPowershellCommand($"-command \"try {{ $path = '{Path}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{sid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"", true);
		}

		public bool SetAccessControl()
		{
			if (GetAccessControl(Path, IsFolder, out var fileSystemSecurity))
			{
				try
				{
					var accessRules = fileSystemSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));

					foreach (var existingRule in accessRules.Cast<FileSystemAccessRule>().Where(x => !x.IsInherited))
						fileSystemSecurity.RemoveAccessRule(existingRule);

					foreach (var rule in AccessControlEntryPrimitiveMappingList.Where(x => !x.IsInherited))
						fileSystemSecurity.AddAccessRule(rule.ToFileSystemAccessRule());

					if (IsFolder)
					{
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(Path), (DirectorySecurity)fileSystemSecurity);
					}
					else
					{
						FileSystemAclExtensions.SetAccessControl(new FileInfo(Path), (FileSecurity)fileSystemSecurity);
					}

					return true;
				}
				catch (UnauthorizedAccessException)
				{
				}
				catch (Exception)
				{
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
				}
				catch (Exception)
				{
				}
			}

			return false;
		}

		public static AccessControlList FromPath(string path, bool isFolder)
		{
			var acl = new AccessControlList()
			{
				Path = path,
				IsFolder = isFolder
			};

			var result = GetAccessControl(path, isFolder, out var fileSystemSecurity);
			if (result)
			{
				var rules = new List<AccessControlEntryPrimitiveMapping>();

				var accessRules = fileSystemSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
				foreach (var accessRule in accessRules)
					rules.Add(AccessControlEntryPrimitiveMapping.FromFileSystemAccessRule((FileSystemAccessRule)accessRule));

				acl.AccessControlEntryPrimitiveMappingList.AddRange(rules);
				acl.OwnerSID = fileSystemSecurity.GetOwner(typeof(SecurityIdentifier)).Value;
				acl.IsAccessControlListProtected = fileSystemSecurity.AreAccessRulesProtected;

				acl.Owner = Principal.FromSid(acl.OwnerSID);

				acl.AccessControlEntriesAdvanced = new(acl.AccessControlEntryPrimitiveMappingList.Select(x => new AccessControlEntryAdvanced(x, isFolder)));
				acl.AccessControlEntries = new(AccessControlEntry.ForAllUsers(acl.AccessControlEntriesAdvanced, isFolder));
			}

			acl.CanReadAccessControl = result;

			return acl;
		}

		private static bool GetAccessControl(string path, bool isFolder, out FileSystemSecurity fileSystemSecurity)
		{
			try
			{
				if (isFolder && Directory.Exists(path))
				{
					fileSystemSecurity = FileSystemAclExtensions.GetAccessControl(new DirectoryInfo(path));

					return true;
				}
				else if (File.Exists(path))
				{
					fileSystemSecurity = FileSystemAclExtensions.GetAccessControl(new FileInfo(path));

					return true;
				}
				else
				{
					// The requested file or folder does not exist
				}
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (Exception)
			{
			}

			fileSystemSecurity = null;

			return false;
		}
		#endregion
	}
}
