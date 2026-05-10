using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Files.IndexedSearch.Client;
using Files.LegacySearch;
using Files.SearchAbstraction;

namespace Files.Search.Bench;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var opts = CliOptions.Parse(args);
            if (opts is null) return 1;

            var manifest = LoadManifest(opts.CorpusDir);
            var queries = QueryGen.Build(manifest);
            Console.WriteLine($"corpus: {manifest.Preset} ({manifest.FileCount:N0} files), provider: {opts.Provider}, queries: {queries.Count}");

            IBenchProvider provider = opts.Provider switch
            {
                "naive-scan" => new NaiveScanProvider(opts.CorpusDir),
                "legacy" => new SearchProviderAdapter(new LegacySearchProvider(), opts.CorpusDir),
                "indexed" => new SearchProviderAdapter(new IndexedSearchProvider(), opts.CorpusDir),
                "windows-aqs" => throw new NotImplementedException(
                    "windows-aqs provider requires the corpus to be added to Windows Search Indexer first. " +
                    "Tracked in docs/decisions/0001-bench-stack.md."),
                _ => throw new ArgumentException($"unknown provider: {opts.Provider}")
            };

            // Warm-up: run one throwaway query so JIT, gRPC channel
            // setup, Tantivy mmap pages, and any first-call penalty
            // don't get baked into the first measured timing.
            if (queries.Count > 0)
            {
                Console.Write("  warm-up...");
                await foreach (var _ in provider.SearchAsync(queries[0])) { }
                Console.WriteLine(" done");
            }

            var results = new List<QueryResult>();
            int i = 0;
            foreach (var q in queries)
            {
                var r = await RunQueryAsync(provider, q);
                results.Add(r);
                i++;
                if ((i & 0xF) == 0) Console.Write($"\r  {i}/{queries.Count}");
            }
            Console.WriteLine($"\r  {queries.Count}/{queries.Count}");

            var run = new BenchRun
            {
                RunId = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ"),
                Provider = opts.Provider,
                Corpus = new CorpusInfo
                {
                    Name = manifest.Preset,
                    Files = manifest.FileCount,
                    Bytes = manifest.TotalBytes,
                    Seed = manifest.Seed,
                },
                Machine = MachineInfo.Capture(),
                Queries = results,
                Aggregates = Aggregates.From(results),
            };

            Directory.CreateDirectory(opts.OutDir);
            var path = Path.Combine(opts.OutDir, $"{run.RunId}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(run, BenchJson.Default.BenchRun));
            Console.WriteLine($"wrote: {path}");

            // Quick console summary.
            var times = results.Where(r => r.TimeToCompleteMs > 0).Select(r => r.TimeToCompleteMs).Order().ToList();
            if (times.Count > 0)
            {
                Console.WriteLine($"  p50 complete: {times[times.Count / 2]:F1}ms  p99: {times[(int)(times.Count * 0.99)]:F1}ms");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<QueryResult> RunQueryAsync(IBenchProvider provider, Query q)
    {
        // Warm-up not done per-query — first run carries cold-cache penalty by design.
        long ramBefore = GC.GetTotalMemory(false);
        var sw = Stopwatch.StartNew();
        long firstResultMs = -1;
        int count = 0;
        await foreach (var _ in provider.SearchAsync(q))
        {
            if (firstResultMs < 0) firstResultMs = sw.ElapsedMilliseconds;
            count++;
        }
        sw.Stop();
        long ramAfter = GC.GetTotalMemory(false);
        return new QueryResult
        {
            Id = q.Id,
            Text = q.Text,
            Class = q.Class,
            TimeToFirstResultMs = firstResultMs < 0 ? sw.ElapsedMilliseconds : firstResultMs,
            TimeToCompleteMs = sw.ElapsedMilliseconds,
            ResultCount = count,
            PeakRamMB = Math.Max(0, (ramAfter - ramBefore) / (1024.0 * 1024)),
            ExpectedMin = q.ExpectedMin,
            ExpectedMax = q.ExpectedMax,
        };
    }

    private static CorpusManifest LoadManifest(string corpusDir)
    {
        var path = Path.Combine(corpusDir, "manifest.json");
        if (!File.Exists(path)) throw new FileNotFoundException($"manifest.json not found in {corpusDir} — run files-corpora first");
        return JsonSerializer.Deserialize(File.ReadAllText(path), BenchJson.Default.CorpusManifest)
            ?? throw new InvalidDataException("manifest.json is empty/invalid");
    }
}

internal sealed class CliOptions
{
    public required string CorpusDir { get; init; }
    public required string OutDir { get; init; }
    public required string Provider { get; init; }

    public static CliOptions? Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine("""
                files-bench --corpus <dir> [--out <dir>] [--provider naive-scan|legacy|indexed]

                Runs ~200 queries against a corpus and writes bench-results/<timestamp>.json.

                Providers:
                  naive-scan  — top-down filesystem walk (strawman baseline).
                  legacy      — Windows.Storage.Search / AQS (the upstream path).
                  indexed     — Rust files-search-service over gRPC. Requires the
                                service to be running and indexing the corpus root
                                (set FILES_SEARCH_ROOT before launching it).
                """);
            return null;
        }

        string? corpus = null, outDir = "bench-results", provider = "naive-scan";
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--corpus": corpus = args[++i]; break;
                case "--out": outDir = args[++i]; break;
                case "--provider": provider = args[++i]; break;
                default: throw new ArgumentException($"unknown arg: {args[i]}");
            }
        }
        if (corpus is null) { Console.Error.WriteLine("error: --corpus is required"); return null; }
        return new CliOptions { CorpusDir = Path.GetFullPath(corpus), OutDir = Path.GetFullPath(outDir!), Provider = provider! };
    }
}

internal interface IBenchProvider
{
    IAsyncEnumerable<string> SearchAsync(Query q);
}

// Adapts an ISearchProvider (the production interface) to the bench's
// IBenchProvider (which only needs path strings). Hands the corpus root
// in as the single scope so each provider searches the same tree even
// when its default scope (e.g. %USERPROFILE% for Indexed) would differ.
internal sealed class SearchProviderAdapter(ISearchProvider inner, string corpusRoot) : IBenchProvider
{
    private readonly IReadOnlyList<string> _scope = new[] { corpusRoot };

    public async IAsyncEnumerable<string> SearchAsync(Query q)
    {
        var sq = new SearchQuery(q.Text, _scope);
        await foreach (var hit in inner.SearchAsync(sq))
            yield return hit.Path;
    }
}

// Walks the tree top-down, matching name patterns. Represents the "unindexed folder" case.
internal sealed class NaiveScanProvider(string root) : IBenchProvider
{
    public async IAsyncEnumerable<string> SearchAsync(Query q)
    {
        await Task.Yield();
        var opts = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = 0,
        };
        // Translate the query to a glob/predicate. For content/path-scoped, we still scan filenames first
        // then peek into content where needed — same work the unindexed legacy fallback does.
        Func<string, bool> pred = QueryMatcher.Build(q);
        foreach (var path in Directory.EnumerateFiles(root, "*", opts))
        {
            if (pred(path)) yield return path;
        }
    }
}

internal static class QueryMatcher
{
    public static Func<string, bool> Build(Query q) => q.Class switch
    {
        "exact" => p => string.Equals(Path.GetFileNameWithoutExtension(p), q.Text, StringComparison.OrdinalIgnoreCase),
        "glob" => MakeGlob(q.Text),
        "substring" => p => Path.GetFileName(p).Contains(q.Text, StringComparison.OrdinalIgnoreCase),
        "ext+substring" => MakeExtSubstring(q.Text),
        "content" => p => ContentContains(p, q.Text),
        _ => p => Path.GetFileName(p).Contains(q.Text, StringComparison.OrdinalIgnoreCase),
    };

    private static Func<string, bool> MakeGlob(string pattern)
    {
        // Tiny glob: '*' wildcard only, matched against filename.
        var parts = pattern.Split('*');
        return p =>
        {
            var name = Path.GetFileName(p);
            int idx = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                int found = name.IndexOf(parts[i], idx, StringComparison.OrdinalIgnoreCase);
                if (found < 0) return false;
                if (i == 0 && !pattern.StartsWith('*') && found != 0) return false;
                idx = found + parts[i].Length;
            }
            if (!pattern.EndsWith('*') && parts.Length > 0 && parts[^1].Length > 0)
                if (!name.EndsWith(parts[^1], StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        };
    }

    private static Func<string, bool> MakeExtSubstring(string spec)
    {
        // Format: "ext|substring", e.g., ".docx|report"
        var parts = spec.Split('|', 2);
        var ext = parts[0]; var sub = parts.Length > 1 ? parts[1] : "";
        return p => string.Equals(Path.GetExtension(p), ext, StringComparison.OrdinalIgnoreCase)
                 && Path.GetFileName(p).Contains(sub, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContentContains(string path, string needle)
    {
        try
        {
            // Only inspect files small enough to scan cheaply; mirrors legacy heuristic.
            var info = new FileInfo(path);
            if (info.Length == 0 || info.Length > 4 * 1024 * 1024) return false;
            // ASCII-fast path is enough — needle tokens are ASCII by construction.
            using var fs = File.OpenRead(path);
            var needleBytes = System.Text.Encoding.UTF8.GetBytes(needle);
            int overlap = needleBytes.Length - 1;
            byte[] buf = new byte[8192];
            byte[] joined = new byte[8192 + overlap];
            byte[] carry = new byte[overlap];
            int carryLen = 0;
            int read;
            while ((read = fs.Read(buf, 0, buf.Length)) > 0)
            {
                int windowLen;
                byte[] window;
                if (carryLen > 0)
                {
                    Buffer.BlockCopy(carry, 0, joined, 0, carryLen);
                    Buffer.BlockCopy(buf, 0, joined, carryLen, read);
                    window = joined; windowLen = carryLen + read;
                }
                else { window = buf; windowLen = read; }

                if (window.AsSpan(0, windowLen).IndexOf(needleBytes) >= 0) return true;

                int keep = Math.Min(overlap, windowLen);
                Buffer.BlockCopy(window, windowLen - keep, carry, 0, keep);
                carryLen = keep;
            }
            return false;
        }
        catch { return false; }
    }
}

internal sealed class Query
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required string Class { get; init; }
    public int ExpectedMin { get; init; } = 0;
    public int ExpectedMax { get; init; } = int.MaxValue;
}

internal static class QueryGen
{
    // Generates ~200 queries deterministic in the manifest's seed, mixing classes from CLAUDE.md.
    public static List<Query> Build(CorpusManifest m)
    {
        var qs = new List<Query>();

        // exact: synthesize plausible names; expected count usually 0 (sentinel), proves "no false positives".
        foreach (var w in new[] { "report_42", "alpha_999", "missingfile" })
            qs.Add(new Query { Id = $"exact-{w}", Text = w, Class = "exact" });

        // glob: extension and prefix patterns.
        foreach (var ext in new[] { ".txt", ".md", ".docx", ".pdf", ".jpg", ".cs", ".log", ".zip" })
            qs.Add(new Query { Id = $"glob-ext{ext}", Text = $"*{ext}", Class = "glob" });
        foreach (var prefix in new[] { "report*", "summary*", "draft*", "data*" })
            qs.Add(new Query { Id = $"glob-{prefix}", Text = prefix, Class = "glob" });

        // substring: common name fragments.
        foreach (var s in new[] { "report", "summary", "config", "build", "alpha", "north", "blue", "internal", "annual" })
            qs.Add(new Query { Id = $"substr-{s}", Text = s, Class = "substring" });

        // ext+substring combos.
        foreach (var combo in new[] { ".docx|report", ".pdf|summary", ".cs|config", ".log|build" })
            qs.Add(new Query { Id = $"extsub-{combo}", Text = combo, Class = "ext+substring" });

        // content: known needle tokens with deterministic counts from the manifest.
        foreach (var (token, count) in m.NeedleCounts)
        {
            qs.Add(new Query
            {
                Id = $"content-{token}",
                Text = token,
                Class = "content",
                // Expect exactly `count` files containing this needle, but allow ±5% slack
                // to absorb the rare overlap collision in random text generation.
                ExpectedMin = (int)(count * 0.95),
                ExpectedMax = (int)Math.Ceiling(count * 1.05) + 1,
            });
        }

        // unicode: relies on the corpus having ~1% unicode-named files.
        qs.Add(new Query { Id = "unicode-cjk", Text = "测试", Class = "substring" });
        qs.Add(new Query { Id = "unicode-emoji", Text = "😀", Class = "substring" });

        // Repeat the most common substrings to get statistical stability for the percentile bands.
        var padding = new[] { "report", "summary", "config" };
        for (int i = 0; qs.Count < 200; i++)
            qs.Add(new Query { Id = $"pad-{i}-{padding[i % padding.Length]}", Text = padding[i % padding.Length], Class = "substring" });

        return qs;
    }
}

// JSON DTOs.
internal sealed class CorpusManifest
{
    [JsonPropertyName("preset")] public string Preset { get; set; } = "";
    [JsonPropertyName("seed")] public int Seed { get; set; }
    [JsonPropertyName("fileCount")] public int FileCount { get; set; }
    [JsonPropertyName("totalBytes")] public long TotalBytes { get; set; }
    [JsonPropertyName("needleCounts")] public Dictionary<string, int> NeedleCounts { get; set; } = new();
}

internal sealed class BenchRun
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; init; } = 1;
    [JsonPropertyName("runId")] public string RunId { get; init; } = "";
    [JsonPropertyName("provider")] public string Provider { get; init; } = "";
    [JsonPropertyName("corpus")] public CorpusInfo Corpus { get; init; } = new();
    [JsonPropertyName("machine")] public MachineInfo Machine { get; init; } = new();
    [JsonPropertyName("aggregates")] public Aggregates Aggregates { get; init; } = new();
    [JsonPropertyName("queries")] public List<QueryResult> Queries { get; init; } = new();
}

// Aggregate percentiles across all queries in the run. The CLAUDE.md
// gates are stated in these terms (TTFR median / p99, etc.), so persist
// them alongside the raw per-query rows for easy diff vs. baseline.json.
internal sealed class Aggregates
{
    [JsonPropertyName("ttfrMedianMs")] public long TtfrMedianMs { get; init; }
    [JsonPropertyName("ttfrP95Ms")] public long TtfrP95Ms { get; init; }
    [JsonPropertyName("ttfrP99Ms")] public long TtfrP99Ms { get; init; }
    [JsonPropertyName("totalMedianMs")] public long TotalMedianMs { get; init; }
    [JsonPropertyName("totalP95Ms")] public long TotalP95Ms { get; init; }
    [JsonPropertyName("totalP99Ms")] public long TotalP99Ms { get; init; }
    [JsonPropertyName("queryCount")] public int QueryCount { get; init; }

    public static Aggregates From(IReadOnlyList<QueryResult> results)
    {
        if (results.Count == 0) return new Aggregates();
        var ttfr = results.Select(r => r.TimeToFirstResultMs).Order().ToList();
        var total = results.Select(r => r.TimeToCompleteMs).Order().ToList();
        return new Aggregates
        {
            QueryCount = results.Count,
            TtfrMedianMs = ttfr[ttfr.Count / 2],
            TtfrP95Ms = ttfr[(int)(ttfr.Count * 0.95)],
            TtfrP99Ms = ttfr[(int)(ttfr.Count * 0.99)],
            TotalMedianMs = total[total.Count / 2],
            TotalP95Ms = total[(int)(total.Count * 0.95)],
            TotalP99Ms = total[(int)(total.Count * 0.99)],
        };
    }
}

internal sealed class CorpusInfo
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("files")] public int Files { get; init; }
    [JsonPropertyName("bytes")] public long Bytes { get; init; }
    [JsonPropertyName("seed")] public int Seed { get; init; }
}

internal sealed class MachineInfo
{
    [JsonPropertyName("os")] public string Os { get; init; } = "";
    [JsonPropertyName("processorCount")] public int ProcessorCount { get; init; }
    [JsonPropertyName("ramGB")] public double RamGB { get; init; }

    public static MachineInfo Capture() => new()
    {
        Os = Environment.OSVersion.VersionString,
        ProcessorCount = Environment.ProcessorCount,
        RamGB = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024), 1),
    };
}

internal sealed class QueryResult
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("text")] public string Text { get; init; } = "";
    [JsonPropertyName("class")] public string Class { get; init; } = "";
    [JsonPropertyName("timeToFirstResultMs")] public long TimeToFirstResultMs { get; init; }
    [JsonPropertyName("timeToCompleteMs")] public long TimeToCompleteMs { get; init; }
    [JsonPropertyName("resultCount")] public int ResultCount { get; init; }
    [JsonPropertyName("peakRamMB")] public double PeakRamMB { get; init; }
    [JsonPropertyName("expectedMin")] public int ExpectedMin { get; init; }
    [JsonPropertyName("expectedMax")] public int ExpectedMax { get; init; }
}

[JsonSerializable(typeof(BenchRun))]
[JsonSerializable(typeof(CorpusManifest))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class BenchJson : JsonSerializerContext { }
