// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Index;
using Files.SearchService.Usn;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.Search.Correctness;

/// <summary>
/// End-to-end correctness: build an index from a real temp directory,
/// then verify indexed results == naive filename-token scan for every query.
///
/// Key invariant tested: no false negatives, no false positives.
/// </summary>
[TestClass]
public class CorpusCorrectnessTests
{
    private static string _root = "";
    private static FileIndex _index = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _root = Path.Combine(Path.GetTempPath(), $"fsix_corpus_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);

        // Deterministic file set covering all interesting cases.
        var files = new[]
        {
            // Standard delimiter-separated names
            "annual_report.pdf",
            "quarterly_report.pdf",
            "quarterly_summary.docx",
            "meeting_notes.txt",
            "config_build.json",
            "build_output.log",
            "server_config.yaml",
            "invoice_2024.pdf",
            "invoice_2024_final.pdf",
            "unrelated.txt",
            // CamelCase
            "AnnualReportFinal.pdf",
            "MyDocumentConfig.docx",
            "BuildOutputFinal.log",
            // Digits
            "report_2024_q1.pdf",
            "v2Final.docx",
            // Unicode
            "测试_report.txt",
            "測試_notes.txt",
            // Long name
            "report_" + new string('a', 120) + ".txt",
            // Multi-extension
            "archive.tar.gz",
            // Nested
            Path.Combine("subfolder", "nested_report.pdf"),
            Path.Combine("subfolder", "nested_summary.txt"),
            Path.Combine("deep", "a", "b", "config.json"),
        };

        // Create the files on disk so UsnJournalReader's fallback walk can find them.
        foreach (var rel in files)
        {
            var fullPath = Path.Combine(_root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, "test");
        }

        // Build index from the UsnJournalReader fallback walk (no USN in dev mode).
        var reader = new UsnJournalReader(_root);
        var records = reader.Enumerate()
            .Select(e => new DocRecord(e.FullPath, e.FileName, e.SizeBytes, e.ModifiedUtc))
            .ToList();
        _index = new FileIndex();
        _index.ReplaceAll(records);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    // ---- Helpers -----------------------------------------------------------

    /// <summary>Naive oracle: files whose tokenized name contains ALL query tokens.</summary>
    private static HashSet<string> NaiveSearch(string query)
    {
        var queryTokens = Tokenizer.Tokenize(query).ToList();
        if (queryTokens.Count == 0) return [];

        return Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
            .Where(path =>
            {
                var fileTokens = Tokenizer.Tokenize(Path.GetFileName(path))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                return queryTokens.All(qt => fileTokens.Contains(qt));
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> IndexSearch(string query) =>
        _index.Search(query, 10_000, [])
            .Select(h => h.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // ---- Tests -------------------------------------------------------------

    [TestMethod]
    [DataRow("report")]
    [DataRow("summary")]
    [DataRow("config")]
    [DataRow("build")]
    [DataRow("invoice")]
    [DataRow("meeting")]
    [DataRow("nested")]
    [DataRow("archive")]
    [DataRow("txt")]
    [DataRow("pdf")]
    public void SingleToken_IndexedMatchesNaive(string query)
    {
        var naive   = NaiveSearch(query);
        var indexed = IndexSearch(query);

        // No false negatives.
        foreach (var path in naive)
            Assert.IsTrue(indexed.Contains(path), $"False negative: '{path}' missing for query '{query}'");

        // No false positives.
        foreach (var path in indexed)
            Assert.IsTrue(naive.Contains(path), $"False positive: '{path}' returned for query '{query}'");
    }

    [TestMethod]
    [DataRow("quarterly report")]
    [DataRow("annual report")]
    [DataRow("config build")]
    [DataRow("invoice 2024")]
    [DataRow("report 2024")]
    public void MultiToken_IndexedMatchesNaive(string query)
    {
        var naive   = NaiveSearch(query);
        var indexed = IndexSearch(query);

        foreach (var path in naive)
            Assert.IsTrue(indexed.Contains(path), $"False negative: '{path}' missing for query '{query}'");

        foreach (var path in indexed)
            Assert.IsTrue(naive.Contains(path), $"False positive: '{path}' returned for query '{query}'");
    }

    [TestMethod]
    public void CamelCase_TokensSearchable_NoFalseNegatives()
    {
        // "AnnualReportFinal.pdf" should appear when searching "annual", "report", or "final".
        var cases = new[] { "annual", "report", "final" };
        foreach (var q in cases)
        {
            var naive   = NaiveSearch(q);
            var indexed = IndexSearch(q);
            foreach (var path in naive)
                Assert.IsTrue(indexed.Contains(path), $"False negative: '{path}' missing for query '{q}'");
        }
    }

    [TestMethod]
    public void Unicode_CJK_NoFalseNegatives()
    {
        var naive   = NaiveSearch("测试");
        var indexed = IndexSearch("测试");

        Assert.IsTrue(naive.Count > 0, "Corpus should have at least one CJK file.");
        foreach (var path in naive)
            Assert.IsTrue(indexed.Contains(path), $"False negative: '{path}' missing for CJK query");
    }

    [TestMethod]
    public void ScopeFilter_SubfolderOnly_NoFalsePositives()
    {
        var subfolder = Path.Combine(_root, "subfolder");
        var hits = _index.Search("report", 10_000, [subfolder]);

        foreach (var hit in hits)
            Assert.IsTrue(hit.Path.StartsWith(subfolder, StringComparison.OrdinalIgnoreCase),
                $"False positive outside scope: '{hit.Path}'");
    }

    [TestMethod]
    public void ScopeFilter_SubfolderOnly_NoFalseNegatives()
    {
        var subfolder = Path.Combine(_root, "subfolder");
        var scoped = _index.Search("report", 10_000, [subfolder])
            .Select(h => h.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Naive walk restricted to subfolder.
        var naiveScoped = Directory.EnumerateFiles(subfolder, "*", SearchOption.AllDirectories)
            .Where(p => Tokenizer.Tokenize(Path.GetFileName(p))
                .Any(t => t.Equals("report", StringComparison.OrdinalIgnoreCase)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in naiveScoped)
            Assert.IsTrue(scoped.Contains(path), $"False negative in scope filter: '{path}'");
    }

    [TestMethod]
    public void UnknownQuery_ReturnsEmpty()
    {
        Assert.AreEqual(0, IndexSearch("zzz_absolutely_nonexistent_token_xqz").Count);
    }

    [TestMethod]
    public void DocCount_MatchesActualFileCount()
    {
        var expectedCount = Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Count();
        // Allow ±0 — every file in the tree should be indexed.
        Assert.AreEqual(expectedCount, (int)_index.DocCount);
    }
}
