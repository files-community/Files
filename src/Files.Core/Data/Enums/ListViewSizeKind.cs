// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Defines constant that specifies the icon/height size in List layout.
	/// </summary>
	public enum ListViewSizeKind
	{
		/// <summary>
		/// The icon/heigh is compact.
		/// </summary>
		[Description("Compact")]
		Compact = 1,

		/// <summary>
		/// The icon/heigh is small.
		/// </summary>
		[Description("Small")]
		Small = 2,

		/// <summary>
		/// The icon/heigh is medium.
		/// </summary>
		[Description("Medium")]
		Medium = 3,

		/// <summary>
		/// The icon/heigh is large.
		/// </summary>
		[Description("Large")]
		Large = 4,

		/// <summary>
		/// The icon/heigh is extra large.
		/// </summary>
		[Description("ExtraLarge")]
		ExtraLarge = 5,
	}
}
