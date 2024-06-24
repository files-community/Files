// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Files.Core.SourceGenerator.Utilities.LightJson
{
	/// <summary>
	/// Enumerates the types of Json values.
	/// </summary>
	public  enum JsonValueType : byte
	{
		/// <summary>
		/// A null value.
		/// </summary>
		Null = 0,

		/// <summary>
		/// A boolean value.
		/// </summary>
		Boolean,

		/// <summary>
		/// A number value.
		/// </summary>
		Number,

		/// <summary>
		/// A string value.
		/// </summary>
		String,

		/// <summary>
		/// An object value.
		/// </summary>
		Object,

		/// <summary>
		/// An array value.
		/// </summary>
		Array,
	}
}
