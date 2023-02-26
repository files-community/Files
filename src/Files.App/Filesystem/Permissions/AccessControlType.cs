namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents type of ACE(Acces Control Entry).
	/// </summary>
	public enum AccessControlType
	{
		/// <summary>
		/// ACE is allow type
		/// </summary>
		Allow = 0,

		/// <summary>
		/// ACE is deny type
		/// </summary>
		Deny = 1
	}
}
