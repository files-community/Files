// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.SourceGenerator.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.CodeAnalysis;


    internal static class ReswParser
    {
        public static IEnumerable<Tuple<string, string?>> GetKeys(AdditionalText file)
        {
            var document = XDocument.Load(file.Path);
            var keys = document
                .Descendants("data")
                .Select(element => Tuple.Create(element.Attribute("name").Value, element.Element("comment")?.Value))
                .Where(key => key.Item1 != null);

            return keys.OrderBy(k => k.Item1);
        }
    }
}
