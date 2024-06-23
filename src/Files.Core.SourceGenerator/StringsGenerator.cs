using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Files.Core.SourceGenerator
{
	[Generator]
	public sealed class StringsGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			var addFiles = context.AdditionalFiles;
			var files = string.Join(", ", addFiles.Where(n => n.Path.EndsWith("en-US\\Resources.resw")).Select(n => n.Path));

			var sb = new StringBuilder();
			sb.AppendLine($"namespace Files.App.Helpers");
			sb.AppendLine("{");
			sb.AppendLine("    public sealed partial class Strings");
			sb.AppendLine("    {");
			sb.AppendLine($"        public const string Found = @\"{files}\";");
			sb.AppendLine("    }");
			sb.AppendLine("}");

			var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);

			context.AddSource("StringsHelper.g.cs", sourceText);
		}
	}
}
