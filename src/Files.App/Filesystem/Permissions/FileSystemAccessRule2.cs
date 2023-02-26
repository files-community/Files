using System.Security.Principal;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Describe
	/// </summary>
	public class FileSystemAccessRule2
	{
		#region Properties
		public System.Security.AccessControl.AccessControlType AccessControlType { get; set; }

		public System.Security.AccessControl.FileSystemRights FileSystemRights { get; set; }

		public string IdentityReference { get; set; }

		public bool IsInherited { get; set; }

		public System.Security.AccessControl.InheritanceFlags InheritanceFlags { get; set; }

		public System.Security.AccessControl.PropagationFlags PropagationFlags { get; set; }
		#endregion

		#region Methods
		public static FileSystemAccessRule2 FromFileSystemAccessRule(FileSystemAccessRule rule)
		{
			return new()
			{
				AccessControlType = (System.Security.AccessControl.AccessControlType)rule.AccessControlType,

				FileSystemRights = (System.Security.AccessControl.FileSystemRights)rule.FileSystemRights,

				IsInherited = rule.IsInherited,

				IdentityReference = rule.IdentityReference,

				InheritanceFlags = (System.Security.AccessControl.InheritanceFlags)rule.InheritanceFlags,

				PropagationFlags = (System.Security.AccessControl.PropagationFlags)rule.PropagationFlags
			};
		}

		public static FileSystemAccessRule2 FromFileSystemAccessRule(System.Security.AccessControl.FileSystemAccessRule rule)
		{
			return new()
			{
				AccessControlType = rule.AccessControlType,
				FileSystemRights = rule.FileSystemRights,
				IsInherited = rule.IsInherited,
				IdentityReference = rule.IdentityReference.Value,
				InheritanceFlags = rule.InheritanceFlags,
				PropagationFlags = rule.PropagationFlags
			};
		}

		public System.Security.AccessControl.FileSystemAccessRule ToFileSystemAccessRule()
		{
			return new(
				identity: new SecurityIdentifier(IdentityReference),
				fileSystemRights: FileSystemRights,
				inheritanceFlags: InheritanceFlags,
				propagationFlags: PropagationFlags,
				type: AccessControlType);
		}
		#endregion
	}
}
