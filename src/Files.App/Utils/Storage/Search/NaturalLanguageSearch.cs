// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Text;

namespace Files.App.Utils.Storage
{
	public static class NaturalLanguageSearch
	{
		private const string PromptTemplate = """
			Convert the file search request into a Windows Advanced Query Syntax (AQS) query. Reply with only the query.
			Allowed terms:
			kind:picture, kind:video, kind:music, kind:document, kind:folder, kind:program
			ext:.pdf (file extension)
			size:>5MB, size:<100KB
			datemodified:today, yesterday, thisweek, lastweek, thismonth, lastmonth, thisyear, lastyear
			datemodified:>=2026-01-01, datemodified:2026-01-01..2026-03-31
			datecreated: (same values as datemodified)
			name:word (text in the file name)
			Combine terms with spaces, or with OR inside parentheses.
			Today is {0:yyyy-MM-dd}.

			Request: pdfs from last month over 5 mb
			Query: ext:.pdf datemodified:lastmonth size:>5MB
			Request: photos i took this year
			Query: kind:picture datecreated:thisyear
			Request: word or excel files with budget in the name
			Query: (ext:.docx OR ext:.xlsx) name:budget
			Request: {1}
			Query:
			""";

		private static readonly SemaphoreSlim _modelLock = new(1, 1);
		private static LanguageModel? _model;

		public static bool IsNaturalLanguageQuery(string query)
			=> !query.StartsWith('$')
			&& !query.Contains(':', StringComparison.Ordinal)
			&& query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3;

		public static bool IsAvailable()
		{
			try
			{
				return LanguageModel.GetReadyState() is AIFeatureReadyState.Ready;
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, "Language model unavailable");
				return false;
			}
		}

		public static async Task<string?> TryTranslateToAqsAsync(string query)
		{
			if (!IsNaturalLanguageQuery(query) || !IsAvailable())
				return null;

			try
			{
				var model = await GetModelAsync();
				var options = new LanguageModelOptions() { Temperature = 0f, TopK = 1 };
				var prompt = string.Format(PromptTemplate, DateTime.Now, query.Trim());

				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				var result = await model.GenerateResponseAsync(prompt, options);
				App.Logger.LogInformation("Natural language search: \"{Query}\" -> \"{Aqs}\" ({Status}, {Elapsed} ms)", query, result.Text, result.Status, stopwatch.ElapsedMilliseconds);

				return result.Status is LanguageModelResponseStatus.Complete ? Sanitize(result.Text) : null;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Natural language search failed");
				return null;
			}
		}

		private static async Task<LanguageModel> GetModelAsync()
		{
			await _modelLock.WaitAsync();
			try
			{
				return _model ??= await LanguageModel.CreateAsync();
			}
			finally
			{
				_modelLock.Release();
			}
		}

		private static string? Sanitize(string? response)
		{
			var aqs = response?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
			if (aqs is null)
				return null;

			if (aqs.StartsWith("Query:", StringComparison.OrdinalIgnoreCase))
				aqs = aqs["Query:".Length..];

			aqs = aqs.Trim().Trim('`', '"');

			return aqs.Length is > 0 and <= 256 && aqs.Contains(':', StringComparison.Ordinal) && !aqs.Contains("tag:", StringComparison.OrdinalIgnoreCase)
				? aqs
				: null;
		}
	}
}
