namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Describe
	/// </summary>
	public class FileSystemAccessRule
	{
		public AccessControlType AccessControlType { get; set; }

		public AccessMaskFlags FileSystemRights { get; set; }

		public string IdentityReference { get; set; }

		public bool IsInherited { get; set; }

		public InheritanceFlags InheritanceFlags { get; set; }

		public PropagationFlags PropagationFlags { get; set; }
	}
}
