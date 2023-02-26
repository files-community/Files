using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents security information of a storage object
	/// </summary>
	public class FilePermissionsManager
	{
		public FilePermissionsManager(FilePermissions permissions)
		{
			FilePath = permissions.FilePath;
			IsFolder = permissions.IsFolder;

			AreAccessRulesProtected = permissions.AreAccessRulesProtected;
			CanReadFilePermissions = permissions.CanReadFilePermissions;

			Owner = UserGroup.FromSid(permissions.OwnerSID);
			CurrentUser = UserGroup.FromSid(permissions.CurrentUserSID);

			AccessRules = new(permissions.AccessRules.Select(x => new FileSystemAccessRuleForUI(x, IsFolder)));
			RulesForUsers = new(RulesForUser.ForAllUsers(AccessRules, IsFolder));
		}

		public string FilePath { get; set; }

		public bool IsFolder { get; set; }

		public bool AreAccessRulesProtected { get; set; }

		public bool CanReadFilePermissions { get; set; }

		public UserGroup Owner { get; private set; }

		public UserGroup CurrentUser { get; private set; }

		public ObservableCollection<FileSystemAccessRuleForUI> AccessRules { get; set; }

		public ObservableCollection<RulesForUser> RulesForUsers { get; private set; }

		public FileSystemRights GetEffectiveRights(UserGroup user)
		{
			var userSids = new List<string> { user.Sid };
			userSids.AddRange(user.Groups);

			FileSystemRights inheritedDenyRights = 0;
			FileSystemRights denyRights = 0;

			FileSystemRights inheritedAllowRights = 0;
			FileSystemRights allowRights = 0;

			foreach (FileSystemAccessRuleForUI Rule in AccessRules.Where(x => userSids.Contains(x.IdentityReference)))
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
	}
}
