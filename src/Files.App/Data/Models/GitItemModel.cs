using LibGit2Sharp;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a model for Git items
	/// </summary>
	internal class GitItemModel
	{
		/// <summary>
		/// Gets or initializes file change kind
		/// </summary>
		/// <remarks>
		/// This is often showed as A(Add), D(Delete), M(Modified), U(Untracked) in VS Code.
		/// </remarks>
		public ChangeKind Status { get; init; }

		/// <summary>
		/// Gets or initializes file change kind humanized string
		/// </summary>
		/// <remarks>
		/// This is often showed as A(Add), D(Delete), M(Modified), U(Untracked) in VS Code.
		/// </remarks>
		public string? StatusHumanized { get; init; }

		/// <summary>
		/// Gets or initializes file last commit information including author, committed date, and SHA.
		/// </summary>
		public Commit? LastCommit { get; init; }

		/// <summary>
		/// Gets or initializes file path
		/// </summary>
		public string? Path { get; init; }
	}
}
