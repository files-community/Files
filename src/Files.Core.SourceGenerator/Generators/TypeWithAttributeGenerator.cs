// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.Core.SourceGenerator.Utilities.SourceGeneratorHelper;

namespace Files.Core.SourceGenerator.Generators
{
	/// <summary>
	/// Base class for generating source code based on types decorated with a specific attribute.
	/// </summary>
	internal abstract class TypeWithAttributeGenerator : IIncrementalGenerator
	{
		/// <summary>
		/// The name of the attribute that this generator operates on.
		/// </summary>
		internal abstract string AttributeName { get; }

		/// <summary>
		/// The full name (including namespace) of the attribute.
		/// </summary>
		private string AttributeFullName => AttributeNamespace + AttributeName;

		/// <summary>
		/// Generates source code for a type decorated with the attribute.
		/// </summary>
		/// <param name="typeSymbol">The symbol representing the type with the attribute.</param>
		/// <param name="attributeList">The list of attributes applied to the type.</param>
		/// <returns>The generated source code as a string, or <c>null</c> if no source code is generated.</returns>
		internal abstract string? TypeWithAttribute(INamedTypeSymbol typeSymbol, ImmutableArray<AttributeData> attributeList);

		/// <summary>
		/// Initializes the source generator.
		/// </summary>
		/// <param name="context">The context for initializing the generator.</param>
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var generatorAttributes = context.SyntaxProvider.ForAttributeWithMetadataName(
				AttributeFullName,
				(_, _) => true,
				(syntaxContext, _) => syntaxContext
			).Combine(context.CompilationProvider);

			context.RegisterSourceOutput(generatorAttributes, (spc, tuple) =>
			{
				var (ga, compilation) = tuple;

				if (compilation.Assembly.GetAttributes().Any(attrData => attrData.AttributeClass?.ToDisplayString() == DisableSourceGeneratorAttribute))
					return;

				if (ga.TargetSymbol is not INamedTypeSymbol symbol)
					return;

				if (TypeWithAttribute(symbol, ga.Attributes) is { } source)
					spc.AddSource(
						// Avoid duplicate names
						$"{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}_{AttributeFullName}.g.cs",
						source);
			});
		}
	}
}
