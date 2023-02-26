using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents an ACL's controller
	/// </summary>
	public class AccessControlListController
	{
		public AccessControlListController(AccessControlList permissions)
		{
			FilePath = permissions.FilePath;
			IsFolder = permissions.IsFolder;

			IsAccessControlListProtected = permissions.IsAccessControlEntryProtected;
			CanReadAccessControl = permissions.CanReadAccessControl;

			Owner = Principal.FromSid(permissions.OwnerSID);
			CurrentUser = Principal.FromSid(permissions.CurrentUserSID);

			AccessControlEntriesAdvanced = new(permissions.AccessRules.Select(x => new AccessControlEntryAdvanced(x, IsFolder)));
			AccessControlEntries = new(AccessControlEntry.ForAllUsers(AccessControlEntriesAdvanced, IsFolder));
		}

		#region Properties
		public string? FilePath { get; set; }

		public bool IsFolder { get; set; }

		public bool IsAccessControlListProtected { get; set; }

		public bool CanReadAccessControl { get; set; }

		public Principal Owner { get; private set; }

		public Principal CurrentUser { get; private set; }

		public ObservableCollection<AccessControlEntryAdvanced> AccessControlEntriesAdvanced { get; set; }

		public ObservableCollection<AccessControlEntry> AccessControlEntries { get; private set; }
		#endregion

		#region Methods
		public AccessControlList ToFilePermissions()
		{
			return new AccessControlList()
			{
				AccessRules = AccessControlEntriesAdvanced.Select(x =>
				{
					AccessControlEntryPrimitive rule = x.ToFileSystemAccessRule();

					return new AccessControlEntryPrimitiveMapping()
					{
						AccessControlType = (System.Security.AccessControl.AccessControlType)rule.AccessControlType,
						FileSystemRights = (System.Security.AccessControl.FileSystemRights)rule.AccessMaskFlags,
						PrincipalSid = rule.PrincipalSid,
						IsInherited = rule.IsInherited,
						InheritanceFlags = (System.Security.AccessControl.InheritanceFlags)rule.InheritanceFlags,
						PropagationFlags = (System.Security.AccessControl.PropagationFlags)rule.PropagationFlags
					};
				})
				.ToList(),

				FilePath = FilePath,
				IsFolder = IsFolder,
				CanReadAccessControl = CanReadAccessControl,
				CurrentUserSID = CurrentUser.Sid,
				OwnerSID = Owner.Sid,
			};
		}
		#endregion
	}
}
