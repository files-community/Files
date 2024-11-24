// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.Core.SourceGenerator.Constants.RealTimeLayoutGenerator;

namespace Files.Core.SourceGenerator.Generators
{
	[Generator]
	public class RealTimeLayoutGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var candidateClasses = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: (node, cancellationToken) => IsValidCandidate(node),
					transform: (context, cancellationToken) => GetCandidateClass(context))
				.Where(candidate => candidate != null);

			context.RegisterSourceOutput(candidateClasses, (context, candidate) =>
			{
				var classDeclaration = candidate!.Value.Class;
				var type = candidate!.Value.Type;
				var className = classDeclaration.Identifier.Text;
				var namespaceName = GetNamespace(classDeclaration);

				var source = GenerateClass(namespaceName, className, type);
				context.AddSource($"{className}.{Guid.NewGuid()}.g.cs", SourceText.From(source, Encoding.UTF8));
			});
		}

		private static bool IsValidCandidate(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax classDeclaration)
			{
				return classDeclaration.BaseList?.Types.Any(baseType => baseType.Type is IdentifierNameSyntax identifier &&
					(identifier.Identifier.Text == SpecificationWindowName || identifier.Identifier.Text == SpecificationControlName)) == true;
			}
			return false;
		}

		private static (ClassDeclarationSyntax Class, SpecificationType Type)? GetCandidateClass(GeneratorSyntaxContext context)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;

			var type = SpecificationType.None;
			foreach (var baseType in classDeclaration.BaseList!.Types)
			{
				if (baseType.Type is IdentifierNameSyntax identifier)
				{
					if (identifier.Identifier.Text == SpecificationWindowName)
						type = SpecificationType.Window;
					else if (identifier.Identifier.Text == SpecificationControlName)
						type = SpecificationType.Control;
				}
			}

			if (type != SpecificationType.None)
				return (classDeclaration, type);

			return null;
		}

		private static string GenerateClass(string namespaceName, string className, SpecificationType type)
		{
			// Namespace
			var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
				.NormalizeWhitespace();

			// Usings
			var usings = new[]
			{
				SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Windows")),
				SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.UI.Xaml")),
				SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.UI.Xaml.Controls"))
			};

			// Field declaration: private IRealTimeLayoutService RTLayoutService;
			var fieldDeclaration = SyntaxFactory.FieldDeclaration(
				SyntaxFactory.VariableDeclaration(
					SyntaxFactory.IdentifierName("IRealTimeLayoutService"))
				.AddVariables(SyntaxFactory.VariableDeclarator("RTLayoutService")))
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

			// Method: InitializeContentLayout
			var initializeContentLayoutMethod = SyntaxFactory.MethodDeclaration(
					SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
					"InitializeContentLayout")
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.WithBody(SyntaxFactory.Block(CreateInitializeContentLayoutBody(type)));

			// Method: UpdateContentLayout
			var updateContentLayoutMethod = SyntaxFactory.MethodDeclaration(
					SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
					"UpdateContentLayout")
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.WithBody(SyntaxFactory.Block(CreateUpdateContentLayoutBody(type)));

			// Class declaration
			var classDeclaration = SyntaxFactory.ClassDeclaration(className)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword))
				.AddMembers(fieldDeclaration, initializeContentLayoutMethod, updateContentLayoutMethod);

			// Add class to namespace
			namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddUsings(usings)
				.AddMembers(namespaceDeclaration)
				.NormalizeWhitespace();

			return compilationUnit.ToFullString();
		}

		private static IEnumerable<StatementSyntax> CreateInitializeContentLayoutBody(SpecificationType type)
		{
			var statements = new List<StatementSyntax>
			{

				SyntaxFactory.ParseStatement("RTLayoutService = Ioc.Default.GetRequiredService<IRealTimeLayoutService>();")
			};

			if (type == SpecificationType.Window)
				statements.Add(SyntaxFactory.ParseStatement("RTLayoutService.UpdateTitleBar(this);"));

			statements.Add(SyntaxFactory.ParseStatement("RTLayoutService.UpdateContent(this);"));
			statements.Add(SyntaxFactory.ParseStatement("RTLayoutService.AddCallback(this, UpdateContentLayout);"));

			return statements;
		}

		private static IEnumerable<StatementSyntax> CreateUpdateContentLayoutBody(SpecificationType type)
		{
			var statements = new List<StatementSyntax>();

			if (type == SpecificationType.Window)
				statements.Add(SyntaxFactory.ParseStatement("RTLayoutService.UpdateTitleBar(this);"));

			statements.Add(SyntaxFactory.ParseStatement("RTLayoutService.UpdateContent(this);"));

			return statements;
		}

		private static string GetNamespace(SyntaxNode node)
		{
			while (node != null)
			{
				if (node is NamespaceDeclarationSyntax namespaceDeclaration)
					return namespaceDeclaration.Name.ToString();
				node = node.Parent!;
			}
			return "GlobalNamespace";
		}
	}
}