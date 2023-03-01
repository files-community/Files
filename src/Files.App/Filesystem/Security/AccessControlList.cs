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
		public AccessControlList(string path, bool isFolder)
		{
			_path = path;
			_isFolder = isFolder;

			AccessControlEntryPrimitiveMappings = new();
		}

		#region Fields and Properties
		public string? OwnerSID { get; private set; }

		public Principal Owner { get; private set; }

		public bool IsAccessControlListProtected { get; private set; }

		public bool CanReadAccessControl { get; private set; }

		public ObservableCollection<AccessControlEntry> AccessControlEntries { get; private set; }

		public List<AccessControlEntryPrimitiveMapping> AccessControlEntryPrimitiveMappings { get; private set; }

		private readonly string _path;

		private readonly bool _isFolder;
		#endregion

		#region Methods
		public bool SetAccessControl()
		{
			if (GetAccessControl(_path, _isFolder, out var fileSystemSecurity))
			{
				try
				{
					var accessRules = fileSystemSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));

					// Remove all existing rules
					foreach (var existingRule in accessRules.Cast<FileSystemAccessRule>().Where(x => !x.IsInherited))
						fileSystemSecurity.RemoveAccessRule(existingRule);

					// Bring back removed rules, including changed rules
					foreach (var rule in AccessControlEntryPrimitiveMappings.Where(x => !x.IsInherited))
						fileSystemSecurity.AddAccessRule(rule.ToFileSystemAccessRule());

					// Save changes
					if (_isFolder)
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(_path), (DirectorySecurity)fileSystemSecurity);
					else
						FileSystemAclExtensions.SetAccessControl(new FileInfo(_path), (FileSecurity)fileSystemSecurity);

					return true;
				}
				catch (UnauthorizedAccessException) { }
				catch (Exception) { }
			}

			return false;
		}

		public bool SetAccessControlProtection(bool isProtected, bool preserveInheritance)
		{
			if (GetAccessControl(_path, _isFolder, out var acs))
			{
				try
				{
					acs.SetAccessRuleProtection(isProtected, preserveInheritance);

					// Save changes
					if (_isFolder)
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(_path), (DirectorySecurity)acs);
					else
						FileSystemAclExtensions.SetAccessControl(new FileInfo(_path), (FileSecurity)acs);

					return true;
				}
				catch (UnauthorizedAccessException) { }
				catch (Exception) { }
			}

			return false;
		}

		public static AccessControlList FromPath(string path, bool isFolder)
		{
			// Get ACL
			var result = GetAccessControl(path, isFolder, out var fileSystemSecurity);
			if (result)
			{
				var ownerSid = fileSystemSecurity.GetOwner(typeof(SecurityIdentifier)).Value;

				var acl = new AccessControlList(path, isFolder)
				{
					IsAccessControlListProtected = fileSystemSecurity.AreAccessRulesProtected,
					OwnerSID = ownerSid,
					Owner = Principal.FromSid(ownerSid),
					CanReadAccessControl = true,
				};

				// Get all ACEs and map them with primitive class
				var accessRules = fileSystemSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
				var rules = new List<AccessControlEntryPrimitiveMapping>();
				foreach (var accessRule in accessRules)
					rules.Add(AccessControlEntryPrimitiveMapping.FromFileSystemAccessRule((FileSystemAccessRule)accessRule));

				acl.AccessControlEntryPrimitiveMappings.AddRange(rules);
				acl.AccessControlEntries = new(rules.Select(x => new AccessControlEntry(x, isFolder)));

				return acl;
			}
			else
			{
				return new(path, isFolder);
			}
		}

		public static bool SetOwner(string path, bool isFolder, string sid)
		{
			if (GetAccessControl(path, isFolder, out var acs))
			{
				try
				{
					// Set owner
					acs.SetOwner(new SecurityIdentifier(sid));

					// Save changes
					if (isFolder)
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(path), (DirectorySecurity)acs);
					else
						FileSystemAclExtensions.SetAccessControl(new FileInfo(path), (FileSecurity)acs);

					return true;
				}
				catch (UnauthorizedAccessException) { }
				catch (Exception) { }
			}

			// If previous operation was unauthorized access, try again through elevated PowerShell
			return Win32API.RunPowershellCommand($"-command \"try {{ $path = '{path}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{sid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"", true);
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
				// The requested file or folder does not exist
				else { }
			}
			catch (UnauthorizedAccessException) { }
			catch (Exception) { }

			fileSystemSecurity = null;

			return false;
		}
		#endregion
	}
}
