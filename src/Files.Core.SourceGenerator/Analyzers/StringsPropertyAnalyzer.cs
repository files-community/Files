// Copyright (c) Files Community
// Licensed under the MIT License.

using static Files.Core.SourceGenerator.Constants.DiagnosticDescriptors;
using static Files.Core.SourceGenerator.Constants.StringsPropertyGenerator;

namespace Files.Core.SourceGenerator.Analyzers
{
	/// <summary>
	/// Analyzer that detects if string literals can be replaced with constants from the <c>Strings</c> class.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	internal sealed class StringsPropertyAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// Gets the supported diagnostics for this analyzer.
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [FSG1002];

		/// <summary>
		/// Initializes the analyzer and registers the syntax node action.
		/// </summary>
		/// <param name="context">The analysis context.</param>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(ctx =>
			{
				var stringsTypeSymbol = ctx.Compilation.GetTypeByMetadataName(StringsMetadataName);

				if (stringsTypeSymbol == null || ctx.CancellationToken.IsCancellationRequested)
					return;

				// Extract constants from the Strings class.
				// Constant values aren't guaranteed to be unique (even though they most likely are).
				var members = stringsTypeSymbol.GetMembers();
				var dictionary = new Dictionary<string, string>(members.Length);
				foreach (var member in members)
				{
					if (member is IFieldSymbol { IsConst: true, ConstantValue: string value } field)
						dictionary[value] = field.Name;
				}

				if (ctx.CancellationToken.IsCancellationRequested)
					return;

				var stringsConstants = dictionary.ToFrozenDictionary();

				ctx.RegisterSyntaxNodeAction(c => AnalyzeNode(c, stringsConstants), SyntaxKind.StringLiteralExpression, SyntaxKind.InterpolatedStringText);
			});
		}

		/// <summary>
		/// Analyzes the syntax node to detect if a string literal can be replaced with a constant from the Strings class.
		/// </summary>
		/// <param name="context">The syntax node analysis context.</param>
		private static void AnalyzeNode(in SyntaxNodeAnalysisContext context, FrozenDictionary<string, string> stringsConstants)
		{
			string literalValue = default!;
			Location locationValue = default!;
			SyntaxNode? parent = null;

			if (context.Node is InterpolatedStringTextSyntax interpolatedStringText)
			{
				literalValue = interpolatedStringText.TextToken.ValueText;
				locationValue = interpolatedStringText.GetLocation();
				parent = interpolatedStringText.Parent?.Parent;
			}
			else if (context.Node is LiteralExpressionSyntax literalExpression)
			{
				literalValue = literalExpression.Token.ValueText;
				locationValue = literalExpression.GetLocation();
				parent = literalExpression.Parent;
			}

			if (string.IsNullOrEmpty(literalValue) ||
				parent is not MemberAccessExpressionSyntax memberAccessExpression ||
				!LocalizedMethodNames.Contains(memberAccessExpression.Name.Identifier.Text))
				return;

			// Check if the literal value matches any of the constants
			if (stringsConstants.TryGetValue(literalValue, out string? name))
			{
				var properties = ImmutableDictionary<string, string>
					.Empty
					.Add(ConstantNameProperty, name);

				var diagnostic = Diagnostic.Create(FSG1002, locationValue, properties!, literalValue, name);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
