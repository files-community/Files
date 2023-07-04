// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Defines constants that specify preview pane status.
	/// </summary>
	public enum PreviewPaneStates
	{
		/// <summary>
		/// No item selected status.
		/// </summary>
		NoItemSelected,

		/// <summary>
		/// No preview available status.
		/// </summary>
		NoPreviewAvailable,

		/// <summary>
		/// No preview or details available status.
		/// </summary>
		NoPreviewOrDetailsAvailable,

		/// <summary>
		/// Preview and details available status.
		/// </summary>
		PreviewAndDetailsAvailable,

		/// <summary>
		/// Loading preview status.
		/// </summary>
		LoadingPreview
	}
}
