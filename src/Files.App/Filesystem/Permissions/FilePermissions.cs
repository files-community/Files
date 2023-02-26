﻿using Files.App.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tulpep.ActiveDirectoryObjectPicker;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents a storage object's security information, such as owner info, access rules
	/// </summary>
	public class FilePermissions
	{
		public FilePermissions()
		{
			AccessRules = new();
		}

		#region Properties
		public string? FilePath { get; set; }

		public bool IsFolder { get; set; }

		public bool CanReadFilePermissions { get; set; }

		public string? OwnerSID { get; set; }

		public string? CurrentUserSID { get; set; }

		public bool AreAccessRulesProtected { get; set; }

		public List<FileSystemAccessRule2> AccessRules { get; set; }
		#endregion

		#region Methods
		public static FilePermissions FromFilePath(string filePath, bool isFolder)
		{
			var filePermissions = new FilePermissions()
			{
				FilePath = filePath,
				IsFolder = isFolder
			};

			var acsResult = GetAccessControl(filePath, isFolder, out var acs);
			if (acsResult)
			{
				var rules = new List<FileSystemAccessRule2>();

				var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));
				foreach (var accessRule in accessRules)
				{
					rules.Add(FileSystemAccessRule2.FromFileSystemAccessRule((System.Security.AccessControl.FileSystemAccessRule)accessRule));
				}

				filePermissions.AccessRules.AddRange(rules);
				filePermissions.OwnerSID = acs.GetOwner(typeof(SecurityIdentifier)).Value;
				filePermissions.AreAccessRulesProtected = acs.AreAccessRulesProtected;
			}

			filePermissions.CanReadFilePermissions = acsResult;

			return filePermissions;
		}

		public bool SetPermissions()
		{
			var acsResult = GetAccessControl(FilePath, IsFolder, out var acs);
			if (acsResult)
			{
				try
				{
					var accessRules = acs.GetAccessRules(true, true, typeof(SecurityIdentifier));

					foreach (var existingRule in accessRules.Cast<System.Security.AccessControl.FileSystemAccessRule>().Where(x => !x.IsInherited))
					{
						acs.RemoveAccessRule(existingRule);
					}

					foreach (var rule in AccessRules.Where(x => !x.IsInherited))
					{
						acs.AddAccessRule(rule.ToFileSystemAccessRule());
					}

					if (IsFolder)
					{
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(FilePath), acs as DirectorySecurity);
					}
					else
					{
						FileSystemAclExtensions.SetAccessControl(new FileInfo(FilePath), acs as FileSecurity);
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

		public bool SetAccessRuleProtection(bool isProtected, bool preserveInheritance)
		{
			var acsResult = GetAccessControl(FilePath, IsFolder, out var acs);
			if (acsResult)
			{
				try
				{
					acs.SetAccessRuleProtection(isProtected, preserveInheritance);

					if (IsFolder)
					{
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(FilePath), acs as DirectorySecurity);
					}
					else
					{
						FileSystemAclExtensions.SetAccessControl(new FileInfo(FilePath), acs as FileSecurity);
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

		public bool SetOwner(string ownerSid)
		{
			var acsResult = GetAccessControl(FilePath, IsFolder, out var acs);
			if (acsResult)
			{
				try
				{
					acs.SetOwner(new SecurityIdentifier(ownerSid));

					if (IsFolder)
					{
						FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(FilePath), acs as DirectorySecurity);
					}
					else
					{
						FileSystemAclExtensions.SetAccessControl(new FileInfo(FilePath), acs as FileSecurity);
					}

					return true;
				}
				catch (UnauthorizedAccessException)
				{
					// User does not have rights to set the owner
				}
				catch (Exception)
				{
				}
			}

			// Set through powershell (admin)
			return Win32API.RunPowershellCommand($"-command \"try {{ $path = '{FilePath}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{ownerSid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"", true);
		}

		private static bool GetAccessControl(string filePath, bool isFolder, out FileSystemSecurity fss)
		{
			try
			{
				if (isFolder && Directory.Exists(filePath))
				{
					fss = FileSystemAclExtensions.GetAccessControl(new DirectoryInfo(filePath));

					return true;
				}
				else if (File.Exists(filePath))
				{
					fss = FileSystemAclExtensions.GetAccessControl(new FileInfo(filePath));

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

		public static Task<string?> OpenObjectPicker(long hwnd)
		{
			return Win32API.StartSTATask(() =>
			{
				DirectoryObjectPickerDialog picker = new()
				{
					AllowedObjectTypes = ObjectTypes.All,
					DefaultObjectTypes = ObjectTypes.Users | ObjectTypes.Groups,
					AllowedLocations = Locations.All,
					DefaultLocations = Locations.LocalComputer,
					MultiSelect = false,
					ShowAdvancedView = true
				};

				picker.AttributesToFetch.Add("objectSid");

				using (picker)
				{
					if (picker.ShowDialog(Win32API.Win32Window.FromLong(hwnd)) == DialogResult.OK)
					{
						try
						{
							var attribs = picker.SelectedObject.FetchedAttributes;
							if (attribs.Any() && attribs[0] is byte[] objectSid)
							{
								return new SecurityIdentifier(objectSid, 0).Value;
							}
						}
						catch
						{
						}
					}
				}

				return null;
			});
		}

		public bool HasPermission(FileSystemRights perm)
			=> GetEffectiveRights().HasFlag(perm);

		public System.Security.AccessControl.FileSystemRights GetEffectiveRights()
		{
			using var user = WindowsIdentity.GetCurrent();
			var userSids = new List<string> { user.User.Value };
			userSids.AddRange(user.Groups.Select(x => x.Value));

			System.Security.AccessControl.FileSystemRights inheritedDenyRights = 0, denyRights = 0;
			System.Security.AccessControl.FileSystemRights inheritedAllowRights = 0, allowRights = 0;

			foreach (var Rule in AccessRules.Where(x => userSids.Contains(x.IdentityReference)))
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
		#endregion
	}
}
