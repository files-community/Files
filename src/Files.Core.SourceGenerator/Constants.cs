// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.SourceGenerator
{
	/// <summary>
	/// Contains various constants used within the source generator.
	/// </summary>
	internal class Constants
	{
		/// <summary>
		/// Contains diagnostic descriptors used for error reporting.
		/// </summary>
		internal class DiagnosticDescriptors
		{
			/// <summary>
			/// Diagnostic descriptor for unsupported types in Windows Registry.
			/// </summary>
			internal static readonly DiagnosticDescriptor FSG1001 = new(
				nameof(FSG1001),
				"Types that are not supported by Windows Registry",
				"Type '{0}' is not supported by Windows Registry",
				"Design",
				DiagnosticSeverity.Error,
				true);
		}

		/// <summary>
		/// Contains constants related to DependencyProperty generation.
		/// </summary>
		internal class DependencyPropertyGenerator
		{
			/// <summary>
			/// The name of the attribute used for DependencyProperty.
			/// </summary>
			internal static readonly string AttributeName = "DependencyPropertyAttribute";
		}
	}
}
