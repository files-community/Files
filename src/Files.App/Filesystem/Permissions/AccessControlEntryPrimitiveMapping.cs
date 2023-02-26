using System.Security.AccessControl;
using System.Security.Principal;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents a primitive ACE for mapping C# classes.
	/// </summary>
	public class AccessControlEntryPrimitiveMapping
	{
		#region Properties
		public System.Security.AccessControl.AccessControlType AccessControlType { get; set; }

		public FileSystemRights FileSystemRights { get; set; }

		public string? PrincipalSid { get; set; }

		public bool IsInherited { get; set; }

		public System.Security.AccessControl.InheritanceFlags InheritanceFlags { get; set; }

		public System.Security.AccessControl.PropagationFlags PropagationFlags { get; set; }
		#endregion

		#region Methods
		public static AccessControlEntryPrimitiveMapping FromAccessControlEntryPrimitive(AccessControlEntryPrimitive accessControl)
		{
			return new()
			{
				AccessControlType = (System.Security.AccessControl.AccessControlType)accessControl.AccessControlType,
				FileSystemRights = (FileSystemRights)accessControl.AccessMaskFlags,
				IsInherited = accessControl.IsInherited,
				PrincipalSid = accessControl.PrincipalSid,
				InheritanceFlags = (System.Security.AccessControl.InheritanceFlags)accessControl.InheritanceFlags,
				PropagationFlags = (System.Security.AccessControl.PropagationFlags)accessControl.PropagationFlags
			};
		}

		public static AccessControlEntryPrimitiveMapping FromFileSystemAccessRule(FileSystemAccessRule accessControl)
		{
			return new()
			{
				AccessControlType = accessControl.AccessControlType,
				FileSystemRights = accessControl.FileSystemRights,
				IsInherited = accessControl.IsInherited,
				PrincipalSid = accessControl.IdentityReference.Value,
				InheritanceFlags = accessControl.InheritanceFlags,
				PropagationFlags = accessControl.PropagationFlags
			};
		}

		public FileSystemAccessRule ToFileSystemAccessRule()
		{
			return new(
				identity: new SecurityIdentifier(PrincipalSid),
				fileSystemRights: FileSystemRights,
				inheritanceFlags: InheritanceFlags,
				propagationFlags: PropagationFlags,
				type: AccessControlType);
		}
		#endregion
	}
}
