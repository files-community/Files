// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.SourceGenerator.Utilities
{
	internal sealed class UsingCollector : CSharpSyntaxWalker
	{
		private readonly HashSet<string> _namespaces;

		public UsingCollector(HashSet<string> namespaces) => _namespaces = namespaces;

		public override void VisitUsingDirective(UsingDirectiveSyntax node) => _namespaces.Add(node.Name!.ToString());
	}
}
