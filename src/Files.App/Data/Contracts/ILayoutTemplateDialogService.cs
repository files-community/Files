// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Asks the user about layout templates.
	/// </summary>
	public interface ILayoutTemplateDialogService
	{
		/// <summary>
		/// Asks for a name and returns it, or null when the user cancels.
		/// </summary>
		Task<string?> AskNameAsync(string title, string primaryButtonText, string name);

		/// <summary>
		/// Asks for file extensions and returns them, or null when the user cancels.
		/// </summary>
		/// <param name="findConflict">Explains why one of the extensions can't be used, or returns null.</param>
		Task<string?> EditFileTypesAsync(string extensions, Func<IEnumerable<string>, string?> findConflict);

		/// <summary>
		/// Asks the user to confirm deleting a template.
		/// </summary>
		Task<bool> ConfirmDeleteAsync(string name);
	}
}
