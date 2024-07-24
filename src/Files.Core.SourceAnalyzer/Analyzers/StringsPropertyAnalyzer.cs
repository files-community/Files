// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Frozen;

namespace Files.Core.SourceAnalyzer.Analyzers
{
	/// <summary>
	/// Analyzer that detects if string literals can be replaced with constants from the <c>Strings</c> class.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	internal sealed class StringsPropertyAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// The diagnostic ID.
		/// </summary>
		public const string DiagnosticId = "FSA1001";

		/// <summary>
		/// The name of the Strings class.
		/// </summary>
		public const string StringsClassName = "Strings";

		/// <summary>
		/// The full metadata name of the Strings class.
		/// </summary>
		public const string StringsMetadataName = $"Files.App.Helpers.{StringsClassName}";

		/// <summary>
		/// The property name for the constant name in the diagnostic properties.
		/// </summary>
		internal const string ConstantNameProperty = nameof(ConstantNameProperty);

		/// <summary>
		/// Represents a collection of constant string names and their corresponding values.
		/// </summary>
		internal static FrozenDictionary<string, string?>? Constants { get; private set; } = null;

		private static readonly LocalizableString _title = "String literal can be replaced with constant";
		private static readonly LocalizableString _messageFormat = $"Replace '{{0}}' with '{StringsClassName}.{{1}}'";
		private static readonly LocalizableString _description = $"Detects string literals that can be replaced with constants from the {StringsClassName} class.";
		private const string Category = "Refactoring";

		/// <summary>
		/// The rule that defines the diagnostic.
		/// </summary>
		private static readonly DiagnosticDescriptor _rule = new(
			id: DiagnosticId,
			title: _title,
			messageFormat: _messageFormat,
			category: Category,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: _description
		);

		/// <summary>
		/// Gets the supported diagnostics for this analyzer.
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

		/// <summary>
		/// Initializes the analyzer and registers the syntax node action.
		/// </summary>
		/// <param name="context">The analysis context.</param>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.StringLiteralExpression, SyntaxKind.InterpolatedStringText);
		}

		/// <summary>
		/// Analyzes the syntax node to detect if a string literal can be replaced with a constant from the Strings class.
		/// </summary>
		/// <param name="context">The syntax node analysis context.</param>
		private void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			var literalValue = string.Empty;
			Location locationValue = default!;

			if (context.Node is InterpolatedStringTextSyntax interpolatedStringText)
			{
				literalValue = interpolatedStringText.TextToken.ValueText;
				locationValue = interpolatedStringText.GetLocation();
			}

			if (context.Node is LiteralExpressionSyntax literalExpression)
			{
				literalValue = literalExpression.Token.ValueText;
				locationValue = literalExpression.GetLocation();
			}

			if (string.IsNullOrEmpty(literalValue))
				return;

			if (Constants is null)
			{
				var semanticModel = context.SemanticModel;
				var compilation = semanticModel.Compilation;

				// Get the type symbol for the Strings class
				var stringsTypeSymbol = compilation.GetTypeByMetadataName(StringsMetadataName);

				if (stringsTypeSymbol == null)
					return;

				// Extract constants from the Strings class
				Constants = stringsTypeSymbol.GetMembers()
					.OfType<IFieldSymbol>()
					.Where(f => f.IsConst)
					.ToDictionary(f => f.Name, f => f.ConstantValue?.ToString())
					.ToFrozenDictionary();
			}


			// Check if the literal value matches any of the constants
			var match = Constants.FirstOrDefault(pair => pair.Value == literalValue);

			if (!match.Equals(default(KeyValuePair<string, string>)))
			{
				var properties = ImmutableDictionary<string, string>
					.Empty
					.Add(ConstantNameProperty, match.Key);

				var diagnostic = Diagnostic.Create(_rule, locationValue, properties!, literalValue, match.Key);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
