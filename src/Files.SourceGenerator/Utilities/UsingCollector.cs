// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Files.SourceGenerator.Utilities
{
	internal class UsingCollector : CSharpSyntaxWalker
	{
		private readonly HashSet<string> _namespaces;

		public UsingCollector(HashSet<string> namespaces) => _namespaces = namespaces;

		public override void VisitUsingDirective(UsingDirectiveSyntax node) => _namespaces.Add(node.Name.ToString());
	}
}
