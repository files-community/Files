// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Text;
using Windows.ApplicationModel;

namespace Files.App.Utils.Storage
{
	public static class NaturalLanguageSearch
	{
		private const string SystemPromptTemplate = """
			Convert the file search request into a Windows Advanced Query Syntax (AQS) query. Reply with only the query.
			Allowed terms:
			kind:picture, kind:video, kind:music, kind:document, kind:folder, kind:program
			ext:.pdf (file extension)
			size:>5MB, size:<100KB
			datemodified:today, yesterday, thisweek, lastweek, thismonth, lastmonth, thisyear, lastyear
			datemodified:>=2026-01-01, datemodified:2026-01-01..2026-03-31
			datecreated: (same values as datemodified)
			name:word (text in the file name)
			Terms separated by spaces must all match, so use each property only once.
			For alternatives of the same property, use OR inside parentheses.
			For a general kind of file, use kind: alone without ext:.
			Today is {0:yyyy-MM-dd}.

			Request: pdfs from last month over 5 mb
			Query: ext:.pdf datemodified:lastmonth size:>5MB
			Request: find image files
			Query: kind:picture
			Request: photos i took this year
			Query: kind:picture datecreated:thisyear
			Request: word or excel files with budget in the name
			Query: (ext:.docx OR ext:.xlsx) name:budget
			""";

		private const string LanguageModelFeatureId = "com.microsoft.windows.ai.languagemodel";

		private static readonly SemaphoreSlim _modelLock = new(1, 1);
		private static LanguageModel? _model;
		private static LanguageModelContext? _context;
		private static DateOnly _contextDate;
		private static bool? _isFeatureUnlocked;

		public static bool IsNaturalLanguageQuery(string query)
			=> !query.StartsWith('$')
			&& !query.Contains(':', StringComparison.Ordinal)
			&& query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3;

		public static bool IsAvailable()
		{
			try
			{
				if (!TryUnlockFeature())
					return false;

				var readyState = LanguageModel.GetReadyState();
				if (readyState is not AIFeatureReadyState.Ready)
					App.Logger.LogInformation("Language model not ready: {State}", readyState);

				return readyState is AIFeatureReadyState.Ready;
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, "Language model unavailable");
				return false;
			}
		}

		public static async Task<string?> TryTranslateToAqsAsync(string query)
		{
			if (!IsNaturalLanguageQuery(query))
			{
				App.Logger.LogInformation("Natural language search skipped: \"{Query}\" is not a natural language query", query);
				return null;
			}

			if (!IsAvailable())
				return null;

			await _modelLock.WaitAsync();
			try
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				var (model, context) = await GetModelAndContextAsync();
				var options = new LanguageModelOptions() { Temperature = 0f, TopK = 1 };

				var result = await model.GenerateResponseAsync(context, $"Request: {query.Trim()}\nQuery:", options);
				App.Logger.LogInformation("Natural language search: \"{Query}\" -> \"{Aqs}\" ({Status}, {Elapsed} ms)", query, result.Text, result.Status, stopwatch.ElapsedMilliseconds);

				return result.Status is LanguageModelResponseStatus.Complete ? Sanitize(result.Text) : null;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Natural language search failed");
				return null;
			}
			finally
			{
				_modelLock.Release();
			}
		}

		/// <summary>
		/// Loads the model and its prompt context in the background so the first search doesn't pay for it.
		/// </summary>
		public static void Prewarm()
		{
			if (!IsAvailable())
				return;

			_ = Task.Run(async () =>
			{
				await _modelLock.WaitAsync();
				try
				{
					await GetModelAndContextAsync();
				}
				catch (Exception ex)
				{
					App.Logger.LogInformation(ex, "Language model prewarm failed");
				}
				finally
				{
					_modelLock.Release();
				}
			});
		}

		private static bool TryUnlockFeature()
		{
			if (_isFeatureUnlocked is { } isUnlocked)
				return isUnlocked;

			var familyName = Package.Current.Id.FamilyName;
			var publisherId = familyName[(familyName.IndexOf('_') + 1)..];
			var access = LimitedAccessFeatures.TryUnlockFeature(
				LanguageModelFeatureId,
				Constants.AutomatedWorkflowInjectionKeys.LanguageModelLafToken,
				$"{publisherId} has registered their use of {LanguageModelFeatureId} with Microsoft and agrees to the terms of use.");

			App.Logger.LogInformation("Language model feature unlock: {Status}", access.Status);

			_isFeatureUnlocked = access.Status is LimitedAccessFeatureStatus.Available or LimitedAccessFeatureStatus.AvailableWithoutToken;
			return _isFeatureUnlocked.Value;
		}

		// Callers must hold _modelLock
		private static async Task<(LanguageModel Model, LanguageModelContext Context)> GetModelAndContextAsync()
		{
			_model ??= await LanguageModel.CreateAsync();

			var today = DateOnly.FromDateTime(DateTime.Now);
			if (_context is null || _contextDate != today)
			{
				_context?.Dispose();
				_context = _model.CreateContext(string.Format(SystemPromptTemplate, DateTime.Now));
				_contextDate = today;
			}

			return (_model, _context);
		}

		/// <summary>
		/// Joins repeated properties with OR, since "ext:.jpg ext:.png" would require both to match.
		/// </summary>
		private static string GroupRepeatedProperties(string aqs)
		{
			if (aqs.Contains('(') || aqs.Contains(" OR ", StringComparison.Ordinal))
				return aqs;

			var groups = aqs.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.GroupBy(term => term.Split(':')[0], StringComparer.OrdinalIgnoreCase)
				.Select(group => group.Count() > 1 ? $"({string.Join(" OR ", group)})" : group.First());

			return string.Join(' ', groups);
		}

		private static string? Sanitize(string? response)
		{
			var aqs = response?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
			if (aqs is null)
				return null;

			if (aqs.StartsWith("Query:", StringComparison.OrdinalIgnoreCase))
				aqs = aqs["Query:".Length..];

			aqs = GroupRepeatedProperties(aqs.Trim().Trim('`', '"'));

			return aqs.Length is > 0 and <= 256 && aqs.Contains(':', StringComparison.Ordinal) && !aqs.Contains("tag:", StringComparison.OrdinalIgnoreCase)
				? aqs
				: null;
		}
	}
}
