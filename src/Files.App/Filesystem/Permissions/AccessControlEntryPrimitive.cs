namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents a primitive ACE.
	/// </summary>
	public class AccessControlEntryPrimitive
	{
		public AccessControlType AccessControlType { get; set; }

		public string PrincipalSid { get; set; }

		public AccessMaskFlags AccessMaskFlags { get; set; }

		public bool IsInherited { get; set; }

		public InheritanceFlags InheritanceFlags { get; set; }

		public PropagationFlags PropagationFlags { get; set; }
	}
}
