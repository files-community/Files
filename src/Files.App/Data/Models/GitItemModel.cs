using LibGit2Sharp;
using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a model for Git items
	/// </summary>
	public sealed class GitItemModel
	{
		/// <summary>
		/// Gets or initializes file change kind
		/// </summary>
		/// <remarks>
		/// This is often showed as A(Added), D(Deleted), M(Modified), U(Untracked) in VS Code.
		/// </remarks>
		public ChangeKind? Status { get; init; }

		/// <summary>
		/// Gets or initializes file change kind icon
		/// </summary>
		/// <remarks>
		/// </remarks>
		public Style? StatusIcon { get; init; }

		/// <summary>
		/// Gets or initializes file change kind humanized string
		/// </summary>
		public string? StatusHumanized { get; init; }

		/// <summary>
		/// Gets or initializes the date of the last commit affecting the item.
		/// </summary>
		public DateTimeOffset? LastCommitDate { get; init; }

		/// <summary>
		/// Gets or initializes the message of the last commit affecting the item.
		/// </summary>
		public string? LastCommitMessage { get; init; }

		/// <summary>
		/// Gets or initializes the author of the last commit affecting the item.
		/// </summary>
		public string? LastCommitAuthor { get; init; }

		/// <summary>
		/// Gets or initializes the SHA of the last commit affecting the item.
		/// </summary>
		public string? LastCommitSha { get; init; }

		/// <summary>
		/// Gets or initializes file path
		/// </summary>
		public string? Path { get; init; }
	}
}
