// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Xml.Linq;

namespace Files.Core.SourceGenerator.Parser
{
	/// <summary>
	/// Provides methods to parse RESW (Resource) files and extract keys with optional comments.
	/// </summary>
	internal static class ReswParser
	{
		/// <summary>
		/// Retrieves all keys and optional comments from the specified RESW file.
		/// </summary>
		/// <param name="file">The additional text representing the RESW file.</param>
		/// <returns>An enumerable of tuples where each tuple contains a key and its associated comment.</returns>
		internal static IEnumerable<Tuple<string, string?>> GetKeys(AdditionalText file)
		{
			var document = XDocument.Load(file.Path);
			var keys = document
				.Descendants("data")
				.Select(element => Tuple.Create(element.Attribute("name")?.Value, element.Element("comment")?.Value))
				.Where(key => key.Item1 is not null) as IEnumerable<Tuple<string, string?>>;

			return keys is not null
				? keys.OrderBy(k => k.Item1)
				: Enumerable.Empty<Tuple<string, string?>>();
		}
	}
}
