namespace Files.Core.Enums
{
	public enum FileNameConflictResolveOptionType : uint
	{
		GenerateNewName = 0,
		ReplaceExisting = 1,
		Skip = 2,
		None = 4
	}
}