// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Files.App.Utils.Storage
{
	public static partial class NaturalLanguageSearch
	{
		private const string SystemPromptTemplate = """
			Extract the filters from a file search request. Reply with only a JSON object whose fields are lists:
			kinds: general kinds of files, from picture, video, music, document, folder, program
			extensions: specific file extensions, such as ".pdf"
			nameContains: text the file name must contain, not types or dates
			modified: when the file was modified
			created: when the file was created
			size: the file size
			Several values in one list mean any of them.
			For modified and created, use today, yesterday, thisweek, lastweek, thismonth, lastmonth, thisyear, lastyear, a range such as 2026-01-01..2026-03-31, or a bound such as >=2026-01-01 or <2026-01-01.
			For size, use a bound such as >5MB or <100KB, or a range such as 1MB..5MB.
			Use created only when the request is about when files were created, downloaded or added; otherwise use modified.
			Leave out fields the request doesn't mention.
			Today is {Today}.
			The request is in {Language}. Keep nameContains in the language of the request.

			Request: pdfs from last month over 5 mb
			{"extensions":[".pdf"],"modified":["lastmonth"],"size":[">5MB"]}
			Request: word or excel files with budget in the name
			{"extensions":[".docx",".xlsx"],"nameContains":["budget"]}
			Request: photos from july
			{"kinds":["picture"],"modified":["{Year}-07-01..{Year}-07-31"]}
			""";

		private static readonly TimeSpan LockTimeout = TimeSpan.FromMilliseconds(500);

		private static readonly SemaphoreSlim _modelLock = new(1, 1);
		private static int _isPrewarming;
		private static LanguageModel? _model;
		private static volatile LanguageModelContext? _context;
		private static DateOnly _contextDate;
		private static string? _contextLanguage;
		private static volatile Availability _availability;

		private enum Availability
		{
			Unknown,
			Available,
			Unavailable,
		}

		public static bool IsNaturalLanguageQuery(string query)
			=> !query.StartsWith('$')
			&& !query.Contains(':', StringComparison.Ordinal)
			&& query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3;

		public static bool IsAvailable()
		{
			if (_availability is not Availability.Unknown)
				return _availability is Availability.Available;

			try
			{
				var readyState = LanguageModel.GetReadyState();
				if (readyState is AIFeatureReadyState.NotReady)
					return false;

				if (readyState is not AIFeatureReadyState.Ready)
					App.Logger.LogInformation("Language model unavailable: {State}", readyState);

				_availability = readyState is AIFeatureReadyState.Ready ? Availability.Available : Availability.Unavailable;
				return readyState is AIFeatureReadyState.Ready;
			}
			catch (Exception ex)
			{
				_availability = Availability.Unavailable;
				App.Logger.LogInformation(ex, "Language model unavailable");
				return false;
			}
		}

		public static async Task<string?> TryTranslateToAqsAsync(string query, CancellationToken token)
		{
			if (!IsNaturalLanguageQuery(query) || !IsAvailable())
				return null;

			try
			{
				// Search the query as typed rather than stall behind a model load
				if (!await _modelLock.WaitAsync(LockTimeout, token))
					return null;
			}
			catch (OperationCanceledException)
			{
				return null;
			}

			try
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				var (model, context) = await GetModelAndContextAsync();
				var aqs = BuildAqs(await GenerateAsync(model, context, query, token));

				App.Logger.LogInformation("Natural language search: {Result} ({Elapsed} ms)", aqs is null ? "no filters" : "translated", stopwatch.ElapsedMilliseconds);

				return aqs;
			}
			catch (OperationCanceledException)
			{
				return null;
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
		/// Loads the model and its prompt context in the background so the search doesn't pay for it.
		/// </summary>
		public static void Prewarm()
		{
			if (_context is not null || Interlocked.Exchange(ref _isPrewarming, 1) is 1)
				return;

			_ = Task.Run(async () =>
			{
				try
				{
					if (!IsAvailable())
						return;

					await _modelLock.WaitAsync();
					try
					{
						await GetModelAndContextAsync();
					}
					finally
					{
						_modelLock.Release();
					}
				}
				catch (Exception ex)
				{
					App.Logger.LogInformation(ex, "Language model prewarm failed");
				}
				finally
				{
					Volatile.Write(ref _isPrewarming, 0);
				}
			});
		}

		// Callers must hold _modelLock
		private static async Task<(LanguageModel Model, LanguageModelContext Context)> GetModelAndContextAsync()
		{
			var model = _model ??= await LanguageModel.CreateAsync();

			var today = DateOnly.FromDateTime(DateTime.Now);
			var language = CultureInfo.CurrentUICulture.EnglishName;
			if (_context is null || _contextDate != today || _contextLanguage != language)
			{
				var prompt = SystemPromptTemplate
					.Replace("{Today}", today.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
					.Replace("{Year}", today.ToString("yyyy"), StringComparison.Ordinal)
					.Replace("{Language}", language, StringComparison.Ordinal);

				_context?.Dispose();
				_context = await Task.Run(() => model.CreateContext(prompt));
				_contextDate = today;
				_contextLanguage = language;
			}

			return (model, _context);
		}

		// Callers must hold _modelLock
		private static async Task<string?> GenerateAsync(LanguageModel model, LanguageModelContext context, string request, CancellationToken token)
		{
			var options = new LanguageModelOptions() { Temperature = 0f, TopK = 1 };
			var result = await model.GenerateResponseAsync(context, $"Request: {request.Trim()}\n", options).AsTask(token);
			if (result.Status is LanguageModelResponseStatus.Complete)
				return result.Text;

			App.Logger.LogInformation("Language model response failed: {Status}, {Error}", result.Status, result.ExtendedError?.Message);

			_context?.Dispose();
			_context = null;
			_model?.Dispose();
			_model = null;
			return null;
		}

		/// <summary>
		/// Builds an AQS query from the filters in the model's JSON reply.
		/// </summary>
		private static string? BuildAqs(string? response)
		{
			var start = response?.IndexOf('{') ?? -1;
			var end = response?.LastIndexOf('}') ?? -1;
			if (start < 0 || end <= start)
				return null;

			try
			{
				using var document = JsonDocument.Parse(response![start..(end + 1)]);
				var filters = document.RootElement;

				var types = GetWords(filters, "kinds").Select(kind => $"kind:{kind}")
					.Concat(GetStrings(filters, "extensions").Select(extension => extension.Trim().TrimStart('.')).Where(IsExtension).Select(extension => $"ext:.{extension}"))
					.ToList();

				var terms = new List<string>();
				if (types.Count > 0)
					terms.Add(types.Count is 1 ? types[0] : $"({string.Join(" OR ", types)})");

				terms.AddRange(GetWords(filters, "nameContains").Select(word => $"name:{word}"));
				terms.AddRange(GetValues(filters, "modified", "datemodified"));
				terms.AddRange(GetValues(filters, "created", "datecreated"));
				terms.AddRange(GetValues(filters, "size", "size"));

				return terms.Count > 0 ? string.Join(' ', terms) : null;
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static IEnumerable<string> GetStrings(JsonElement filters, string name)
		{
			if (filters.ValueKind is not JsonValueKind.Object || !filters.TryGetProperty(name, out var property))
				return [];

			return property.ValueKind switch
			{
				JsonValueKind.Array => property.EnumerateArray().Where(item => item.ValueKind is JsonValueKind.String).Select(item => item.GetString()!).ToList(),
				JsonValueKind.String => [property.GetString()!],
				_ => [],
			};
		}

		private static IEnumerable<string> GetWords(JsonElement filters, string name)
			=> GetStrings(filters, name)
				.SelectMany(value => value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
				.Where(word => WordRegex().IsMatch(word));

		private static IEnumerable<string> GetValues(JsonElement filters, string name, string property)
		{
			var terms = GetStrings(filters, name).Where(value => ValueRegex().IsMatch(value)).Select(value => $"{property}:{value}").ToList();
			return terms.Count > 1 ? [$"({string.Join(" OR ", terms)})"] : terms;
		}

		private static bool IsExtension(string extension)
			=> extension.Length > 0 && extension.Split('.').All(part => WordRegex().IsMatch(part));

		[GeneratedRegex(@"^[\p{L}\p{N}_-]+$")]
		private static partial Regex WordRegex();

		[GeneratedRegex(@"^[\p{L}\p{N}<>=.-]+$")]
		private static partial Regex ValueRegex();
	}
}
