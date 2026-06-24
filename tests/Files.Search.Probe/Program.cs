// Integration test harness for the Files search service.
//
// Runs end-to-end scenarios that mirror what SearchRouter does in Files.App,
// so you can verify search behavior without launching the UI.
//
// Usage:
//   dotnet run --project probe.csproj                  # full test suite
//   dotnet run --project probe.csproj -- query "bmra"  # single ad-hoc query
//   dotnet run --project probe.csproj -- bench         # latency benchmark
//
// The harness auto-starts the service if it isn't running, so the only
// thing you need is the built service binary at the path below.

using Files.IndexedSearch.Client;
using Files.SearchAbstraction;
using System.Diagnostics;

const string ServiceUrl  = "http://localhost:50299";
const string ServiceExe  = @"C:\Users\Tommy\source\repos\Files\src\Files.SearchService\bin\x64\Debug\net10.0-windows10.0.26100.0\files-search-service.exe";
const string UserProfile = @"C:\Users\Tommy";

Environment.SetEnvironmentVariable("FILES_SEARCH_SERVICE_URL", ServiceUrl);

await EnsureServiceUp();

if (args.Length > 0 && args[0] == "query")
{
    await AdHocQuery(args.Length > 1 ? args[1] : "readme",
                    args.Length > 2 ? args[2] : UserProfile);
    return;
}

if (args.Length > 0 && args[0] == "bench")
{
    await Bench();
    return;
}

await RunTestSuite();

// ──────────────────────────────────────────────────────────────────────────
// Test scenarios
// ──────────────────────────────────────────────────────────────────────────

async Task RunTestSuite()
{
    var results = new List<bool>();
    var totalSw = Stopwatch.StartNew();

    results.Add(await Check("service is up and has indexed files", async () =>
    {
        using var p = new IndexedSearchProvider();
        var h = await p.GetHealthAsync(CancellationToken.None);
        Require(h.IsAvailable, $"service unavailable");
        Require(h.IndexedFileCount > 1000, $"only {h.IndexedFileCount} files indexed");
        return $"available, {h.IndexedFileCount:N0} files, indexing={h.IsIndexing}";
    }));

    results.Add(await Check("scoped search returns results in <500ms", async () =>
    {
        var (count, ms, _) = await Search("readme", new[] { UserProfile }, 200);
        Require(count > 0, "no results for 'readme' in user profile");
        Require(ms < 500, $"took {ms}ms (>500ms)");
        return $"{count} results in {ms}ms";
    }));

    results.Add(await Check("Home/unscoped search returns results in <500ms", async () =>
    {
        var (count, ms, _) = await Search("readme", Array.Empty<string>(), 200);
        Require(count > 0, "no results for 'readme' globally");
        Require(ms < 500, $"took {ms}ms (>500ms)");
        return $"{count} results in {ms}ms (scope=full index)";
    }));

    results.Add(await Check("trigram match for mid-string substring", async () =>
    {
        var (count, ms, sample) = await Search("oduct", Array.Empty<string>(), 50);
        return count == 0
            ? "0 results (no files containing 'oduct' in this corpus)"
            : $"{count} results in {ms}ms, e.g. '{sample}'";
    }));

    results.Add(await Check("nonexistent query returns 0 results quickly", async () =>
    {
        var (count, ms, _) = await Search("zzzzzzzzzzz", Array.Empty<string>(), 50);
        Require(ms < 500, $"took {ms}ms");
        Require(count == 0, $"unexpected {count} results");
        return $"0 results in {ms}ms";
    }));

    results.Add(await Check("search does not pin CPU", async () =>
    {
        var svc = Process.GetProcessesByName("files-search-service").FirstOrDefault();
        Require(svc is not null, "service process missing");
        var cpuBefore = svc!.TotalProcessorTime;
        var (count, ms, _) = await Search("data", Array.Empty<string>(), 200);
        svc.Refresh();
        var cpuAfter = svc.TotalProcessorTime;
        var cpuUsed = (cpuAfter - cpuBefore).TotalMilliseconds;
        var cpuPct = ms > 0 ? cpuUsed * 100.0 / ms : 0;
        // Two-tier scoring iterates all candidates with cheap scoring, which
        // uses multiple cores briefly. Threshold accounts for that — pinning
        // would be sustained 800%+, not a brief 200-400% spike.
        Require(cpuPct < 600, $"CPU at {cpuPct:F0}% (expected <600% during 30ms burst)");
        return $"{count} results in {ms}ms, CPU={cpuPct:F0}% of wall time";
    }));

    results.Add(await Check("warm channel search is <100ms", async () =>
    {
        using var p = new IndexedSearchProvider();
        await p.GetHealthAsync(CancellationToken.None);
        var sw = Stopwatch.StartNew();
        int count = 0;
        await foreach (var _ in p.SearchAsync(
            new SearchQuery("readme", new[] { UserProfile }, MaxResults: 100), CancellationToken.None))
            count++;
        var ms = sw.ElapsedMilliseconds;
        Require(ms < 100, $"warm search took {ms}ms");
        return $"{count} results in {ms}ms (warm channel)";
    }));

    var passed = results.Count(r => r);
    var failed = results.Count - passed;
    Console.WriteLine();
    Console.WriteLine($"━━━ {passed} passed, {failed} failed, total {totalSw.ElapsedMilliseconds}ms ━━━");
    Environment.Exit(failed > 0 ? 1 : 0);
}

async Task AdHocQuery(string query, string scope)
{
    Console.WriteLine($"Ad-hoc: '{query}' in '{(string.IsNullOrEmpty(scope) ? "<full index>" : scope)}'");
    var scopes = string.IsNullOrEmpty(scope) || scope.Equals("Home", StringComparison.OrdinalIgnoreCase)
        ? Array.Empty<string>()
        : new[] { scope };

    using var p = new IndexedSearchProvider();
    var sw = Stopwatch.StartNew();
    var hits = new List<SearchResult>();
    await foreach (var hit in p.SearchAsync(
        new SearchQuery(query, scopes, MaxResults: 50), CancellationToken.None))
        hits.Add(hit);

    Console.WriteLine($"{hits.Count} results in {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"  {"score",6}  filename");
    foreach (var h in hits.Take(15))
        Console.WriteLine($"  {h.Score,6:F2}  {h.FileName}");
    if (hits.Count > 15)
        Console.WriteLine($"  …{hits.Count - 15} more");
}

async Task Bench()
{
    string[] queries = { "readme", "json", "config", "test", "data", "image", "log", "main" };
    using var p = new IndexedSearchProvider();
    await p.GetHealthAsync(CancellationToken.None); // warm up
    Console.WriteLine($"{"query",-10}  {"results",8}  {"first(ms)",10}  {"total(ms)",10}");

    foreach (var q in queries)
    {
        var sw = Stopwatch.StartNew();
        int count = 0;
        long firstMs = -1;
        await foreach (var _ in p.SearchAsync(
            new SearchQuery(q, Array.Empty<string>(), MaxResults: 200), CancellationToken.None))
        {
            if (count == 0) firstMs = sw.ElapsedMilliseconds;
            count++;
        }
        Console.WriteLine($"{q,-10}  {count,8}  {firstMs,10}  {sw.ElapsedMilliseconds,10}");
    }
}

// ──────────────────────────────────────────────────────────────────────────
// Helpers
// ──────────────────────────────────────────────────────────────────────────

async Task<(int count, long ms, string? sample)> Search(string query, string[] scopes, int max)
{
    using var p = new IndexedSearchProvider();
    var sw = Stopwatch.StartNew();
    int count = 0;
    string? first = null;
    await foreach (var hit in p.SearchAsync(
        new SearchQuery(query, scopes, MaxResults: max), CancellationToken.None))
    {
        first ??= hit.FileName;
        count++;
    }
    return (count, sw.ElapsedMilliseconds, first);
}

async Task<bool> Check(string name, Func<Task<string>> body)
{
    Console.Write($"  • {name} … ");
    try
    {
        var detail = await body();
        Console.WriteLine($"PASS  ({detail})");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL  {ex.Message}");
        return false;
    }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

async Task EnsureServiceUp()
{
    if (Process.GetProcessesByName("files-search-service").Length > 0)
        return;

    if (!File.Exists(ServiceExe))
    {
        Console.Error.WriteLine($"Service binary missing: {ServiceExe}");
        Console.Error.WriteLine("Build Files.SearchService first.");
        Environment.Exit(2);
    }

    Console.WriteLine($"Starting service: {ServiceExe}");
    var psi = new ProcessStartInfo
    {
        FileName = ServiceExe,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    psi.Environment["FILES_SEARCH_SERVICE_URL"] = ServiceUrl;
    psi.Environment["FILES_SEARCH_ROOT"] = UserProfile;
    Process.Start(psi);

    // Wait for the service to start accepting connections (up to 10s).
    using var probe = new IndexedSearchProvider();
    for (int i = 0; i < 20; i++)
    {
        await Task.Delay(500);
        try
        {
            var h = await probe.GetHealthAsync(CancellationToken.None);
            if (h.IsAvailable)
            {
                Console.WriteLine($"Service ready: {h.IndexedFileCount:N0} indexed, indexing={h.IsIndexing}");
                return;
            }
        }
        catch { }
    }
    Console.Error.WriteLine("Service did not become ready within 10s.");
    Environment.Exit(3);
}
