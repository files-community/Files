namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents a storage object's ACE(Access Control Entry) constructed by the classes in Files.App.Filesystem.Permissions
	/// </summary>
	public class FileSystemAccessRule
	{
		public AccessControlType AccessControlType { get; set; }

		public FileSystemRights FileSystemRights { get; set; }

		public string IdentityReference { get; set; }

		public bool IsInherited { get; set; }

		public InheritanceFlags InheritanceFlags { get; set; }

		public PropagationFlags PropagationFlags { get; set; }
	}
}
