// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	/// <summary>
	/// Defines constants that specify how to handle previous selections when performing a new selection.
	/// </summary>
	public enum DragSelectionKind
	{
		/// <summary>
		/// Clears the prevous selection and starts a new selection.
		/// </summary>
		IgnorePreviousSelection,

		/// <summary>
		/// Inverts the previous selection and selects/deselects items accordingly.
		/// </summary>
		InvertPreviousSelection,

		/// <summary>
		/// Keeps the previous selection and selects additional items.
		/// </summary>
		ExtendPreviousSelection,
	}
}
