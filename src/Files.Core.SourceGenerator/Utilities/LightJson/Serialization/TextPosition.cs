// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Files.Core.SourceGenerator.Utilities.LightJson.Serialization
{
	/// <summary>
	/// Represents a position within a plain text resource.
	/// </summary>
	public  struct TextPosition
	{
		/// <summary>
		/// The column position, 0-based.
		/// </summary>
		public  long Column;

		/// <summary>
		/// The line position, 0-based.
		/// </summary>
		public  long Line;
	}
}
