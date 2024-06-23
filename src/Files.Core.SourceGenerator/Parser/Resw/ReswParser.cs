// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Files.Core.SourceGenerator.Parser.Resw
{
    internal static class ReswParser
    {
        public static IEnumerable<string> GetKeys(AdditionalText file)
        {
            var document = XDocument.Load(file.Path);
            var keys = document
                .Descendants("data")
                .Select(element => element.Attribute("name").Value)
                .Where(key => key != null);

            return keys ?? [];
        }
    }
}
