using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents a storage object's security information manager.
	/// </summary>
	public class FilePermissionsManager
	{
		public FilePermissionsManager(FilePermissions permissions)
		{
			FilePath = permissions.FilePath;
			IsFolder = permissions.IsFolder;

			AreAccessRulesProtected = permissions.AreAccessRulesProtected;
			CanReadFilePermissions = permissions.CanReadFilePermissions;

			Owner = Principal.FromSid(permissions.OwnerSID);
			CurrentUser = Principal.FromSid(permissions.CurrentUserSID);

			AccessRules = new(permissions.AccessRules.Select(x => new AccessControlEntryAdvanced(x, IsFolder)));
			RulesForUsers = new(AccessControlEntry.ForAllUsers(AccessRules, IsFolder));
		}

		#region Properties
		public string? FilePath { get; set; }

		public bool IsFolder { get; set; }

		public bool AreAccessRulesProtected { get; set; }

		public bool CanReadFilePermissions { get; set; }

		public Principal Owner { get; private set; }

		public Principal CurrentUser { get; private set; }

		public ObservableCollection<AccessControlEntryAdvanced> AccessRules { get; set; }

		public ObservableCollection<AccessControlEntry> RulesForUsers { get; private set; }
		#endregion

		#region Methods
		public AccessMask GetEffectiveRights(Principal user)
		{
			var userSids = new List<string> { user.Sid };
			userSids.AddRange(user.Groups);

			AccessMask inheritedDenyRights = 0;
			AccessMask denyRights = 0;

			AccessMask inheritedAllowRights = 0;
			AccessMask allowRights = 0;

			foreach (AccessControlEntryAdvanced Rule in AccessRules.Where(x => userSids.Contains(x.PrincipalSid)))
			{
				if (Rule.AccessControlType == AccessControlType.Deny)
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
				else if (Rule.AccessControlType == AccessControlType.Allow)
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

		public FilePermissions ToFilePermissions()
		{
			return new FilePermissions()
			{
				AccessRules = AccessRules.Select(x =>
				{
					FileSystemAccessRule rule = x.ToFileSystemAccessRule();

					return new FileSystemAccessRule2()
					{
						AccessControlType = (System.Security.AccessControl.AccessControlType)rule.AccessControlType,
						FileSystemRights = (System.Security.AccessControl.FileSystemRights)rule.FileSystemRights,
						IdentityReference = rule.IdentityReference,
						InheritanceFlags = (System.Security.AccessControl.InheritanceFlags)rule.InheritanceFlags,
						IsInherited = rule.IsInherited,
						PropagationFlags = (System.Security.AccessControl.PropagationFlags)rule.PropagationFlags
					};
				})
				.ToList(),

				FilePath = FilePath,
				IsFolder = IsFolder,
				CanReadFilePermissions = CanReadFilePermissions,
				CurrentUserSID = CurrentUser.Sid,
				OwnerSID = Owner.Sid,
			};
		}
		#endregion
	}
}
