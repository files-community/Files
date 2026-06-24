// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Index;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.Search.Correctness;

/// <summary>
/// Correctness tests for <see cref="FileIndex"/>.
///
/// Core invariant: for a query Q, the index returns exactly the set of
/// documents whose filename contains all of Q's tokens (AND semantics).
/// No false positives, no false negatives for token-exact queries.
/// </summary>
[TestClass]
public class FileIndexTests
{
    private static FileIndex BuildIndex(params (string path, string name)[] files)
    {
        var idx = new FileIndex();
        var records = files
            .Select(f => new DocRecord(f.path, f.name, 0UL, DateTime.UtcNow))
            .ToList();
        idx.ReplaceAll(records);
        return idx;
    }

    private static IReadOnlyList<QueryHit> Search(FileIndex idx, string query, params string[] scopes) =>
        idx.Search(query, 10_000, scopes);

    // ---- Basic retrieval ---------------------------------------------------

    [TestMethod]
    public void SingleToken_FindsMatchingFile()
    {
        var idx = BuildIndex(
            (@"C:\root\annual_report.pdf", "annual_report.pdf"),
            (@"C:\root\quarterly_summary.docx", "quarterly_summary.docx"));

        var hits = Search(idx, "report");

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual("annual_report.pdf", hits[0].FileName);
    }

    [TestMethod]
    public void SingleToken_NoMatch_ReturnsEmpty()
    {
        var idx = BuildIndex((@"C:\root\file.txt", "file.txt"));
        Assert.AreEqual(0, Search(idx, "zzz_nonexistent").Count);
    }

    [TestMethod]
    public void EmptyQuery_ReturnsEmpty()
    {
        var idx = BuildIndex((@"C:\root\file.txt", "file.txt"));
        Assert.AreEqual(0, Search(idx, "").Count);
    }

    // ---- AND semantics for multi-token queries -----------------------------

    [TestMethod]
    public void MultiToken_And_OnlyFilesWithAllTokens()
    {
        var idx = BuildIndex(
            (@"C:\root\annual_report.pdf", "annual_report.pdf"),
            (@"C:\root\quarterly_report.pdf", "quarterly_report.pdf"),
            (@"C:\root\annual_summary.docx", "annual_summary.docx"));

        // "annual report" → both "annual" AND "report" required
        var hits = Search(idx, "annual report");

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual("annual_report.pdf", hits[0].FileName);
    }

    [TestMethod]
    public void MultiToken_MissingOneToken_ReturnsEmpty()
    {
        var idx = BuildIndex((@"C:\root\report.txt", "report.txt"));
        // "annual" is not in "report.txt" → no result
        Assert.AreEqual(0, Search(idx, "annual report").Count);
    }

    // ---- No false positives ------------------------------------------------

    [TestMethod]
    public void NoFalsePositives_UnrelatedFilesNotReturned()
    {
        var idx = BuildIndex(
            (@"C:\root\report.pdf", "report.pdf"),
            (@"C:\root\invoice.pdf", "invoice.pdf"),
            (@"C:\root\summary.txt", "summary.txt"));

        var hits = Search(idx, "report").Select(h => h.FileName).ToHashSet();

        Assert.IsTrue(hits.Contains("report.pdf"));
        Assert.IsFalse(hits.Contains("invoice.pdf"));
        Assert.IsFalse(hits.Contains("summary.txt"));
    }

    // ---- No false negatives ------------------------------------------------

    [TestMethod]
    public void AllMatchingFiles_AreReturned()
    {
        var idx = BuildIndex(
            (@"C:\root\report_q1.pdf", "report_q1.pdf"),
            (@"C:\root\report_q2.pdf", "report_q2.pdf"),
            (@"C:\root\report_q3.pdf", "report_q3.pdf"),
            (@"C:\root\unrelated.txt", "unrelated.txt"));

        var hits = Search(idx, "report");
        var names = hits.Select(h => h.FileName).ToHashSet();

        Assert.IsTrue(names.Contains("report_q1.pdf"));
        Assert.IsTrue(names.Contains("report_q2.pdf"));
        Assert.IsTrue(names.Contains("report_q3.pdf"));
        Assert.IsFalse(names.Contains("unrelated.txt"));
    }

    // ---- Scope filtering ---------------------------------------------------

    [TestMethod]
    public void ScopeFilter_ExcludesOutOfScopePaths()
    {
        var idx = BuildIndex(
            (@"C:\root\folder1\report.txt", "report.txt"),
            (@"C:\root\folder2\report.txt", "report.txt"));

        var hits = Search(idx, "report", @"C:\root\folder1");

        Assert.AreEqual(1, hits.Count);
        Assert.IsTrue(hits[0].Path.StartsWith(@"C:\root\folder1", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ScopeFilter_EmptyScope_ReturnsAll()
    {
        var idx = BuildIndex(
            (@"C:\root\folder1\report.txt", "report.txt"),
            (@"C:\root\folder2\report.txt", "report.txt"));

        // No scope = no filtering.
        var hits = Search(idx, "report");
        Assert.AreEqual(2, hits.Count);
    }

    [TestMethod]
    public void ScopeFilter_MultipleScopes_UnionSemantics()
    {
        var idx = BuildIndex(
            (@"C:\root\a\report.txt", "report.txt"),
            (@"C:\root\b\report.txt", "report.txt"),
            (@"C:\root\c\report.txt", "report.txt"));

        var hits = Search(idx, "report", @"C:\root\a", @"C:\root\b");
        Assert.AreEqual(2, hits.Count);
    }

    // ---- CamelCase splitting -----------------------------------------------

    [TestMethod]
    public void CamelCase_TokensSearchable()
    {
        var idx = BuildIndex((@"C:\root\MyDocumentFinal.docx", "MyDocumentFinal.docx"));

        Assert.AreEqual(1, Search(idx, "document").Count);
        Assert.AreEqual(1, Search(idx, "my").Count);
        Assert.AreEqual(1, Search(idx, "final").Count);
    }

    [TestMethod]
    public void CamelCase_MultiToken_FindsFile()
    {
        var idx = BuildIndex((@"C:\root\AnnualReportFinal.pdf", "AnnualReportFinal.pdf"));
        Assert.AreEqual(1, Search(idx, "annual report").Count);
    }

    // ---- Unicode -----------------------------------------------------------

    [TestMethod]
    public void Unicode_CJK_FindsFile()
    {
        var idx = BuildIndex((@"C:\root\测试_file.txt", "测试_file.txt"));
        Assert.AreEqual(1, Search(idx, "测试").Count);
    }

    [TestMethod]
    public void Unicode_FilenameWithCJKAndLatin_BothTokensSearchable()
    {
        var idx = BuildIndex((@"C:\root\测试_report.pdf", "测试_report.pdf"));
        Assert.AreEqual(1, Search(idx, "report").Count);
        Assert.AreEqual(1, Search(idx, "测试").Count);
    }

    // ---- Incremental updates -----------------------------------------------

    [TestMethod]
    public void Upsert_NewFile_IsSearchable()
    {
        var idx = new FileIndex();
        idx.ReplaceAll([]);
        idx.Upsert(@"C:\root\new_report.txt", "new_report.txt", 0, DateTime.UtcNow);

        Assert.AreEqual(1, Search(idx, "report").Count);
        Assert.AreEqual(1, Search(idx, "new").Count);
    }

    [TestMethod]
    public void Upsert_ExistingPath_UpdatesFile()
    {
        var idx = BuildIndex((@"C:\root\file.txt", "old_name.txt"));
        // Upsert replaces the existing doc.
        idx.Upsert(@"C:\root\file.txt", "new_name.txt", 0, DateTime.UtcNow);

        Assert.AreEqual(0, Search(idx, "old").Count);
        Assert.AreEqual(1, Search(idx, "new").Count);
    }

    [TestMethod]
    public void Delete_RemovedFile_NoLongerReturned()
    {
        var idx = BuildIndex((@"C:\root\delete_me.txt", "delete_me.txt"));
        idx.Delete(@"C:\root\delete_me.txt");

        Assert.AreEqual(0, Search(idx, "delete").Count);
    }

    [TestMethod]
    public void Delete_UnknownPath_IsNoOp()
    {
        var idx = BuildIndex((@"C:\root\file.txt", "file.txt"));
        idx.Delete(@"C:\root\nonexistent.txt"); // Should not throw.
        Assert.AreEqual(1, Search(idx, "file").Count);
    }

    // ---- Result scoring / ordering -----------------------------------------

    [TestMethod]
    public void ExactMatch_RankedFirst()
    {
        var idx = BuildIndex(
            (@"C:\root\report_annual.pdf", "report_annual.pdf"),
            (@"C:\root\report.pdf", "report.pdf"),        // exact
            (@"C:\root\annual_report.pdf", "annual_report.pdf"));

        var hits = Search(idx, "report.pdf");

        // The exact match ("report.pdf") should have the highest score.
        Assert.AreEqual("report.pdf", hits[0].FileName);
        Assert.AreEqual(1.0f, hits[0].Score);
    }

    [TestMethod]
    public void MaxResults_CapsResultCount()
    {
        var idx = new FileIndex();
        var records = Enumerable.Range(0, 50)
            .Select(i => new DocRecord($@"C:\root\report_{i}.txt", $"report_{i}.txt", 0, DateTime.UtcNow))
            .ToList();
        idx.ReplaceAll(records);

        var hits = idx.Search("report", 10, []);
        Assert.AreEqual(10, hits.Count);
    }

    [TestMethod]
    public void MaxResults_Truncation_KeepsTopByScore()
    {
        // Regression: previously the truncation happened BEFORE sorting by score,
        // so the top-N was the first-N candidates in doc-ID order — meaning a
        // high-scoring match enrolled late could be silently dropped while
        // low-quality substring matches filled the result list. Score-then-truncate
        // ensures the best matches always survive the cut.
        var idx = new FileIndex();
        var records = new List<DocRecord>();

        // 99 low-quality matches added FIRST (lower doc IDs). For query "report"
        // these score 0.4 — "report" is a substring of the filename but the
        // filename doesn't start with it and "report" isn't a clean prefix of
        // a single token either (since they're all "zzzreport...").
        for (int i = 0; i < 99; i++)
            records.Add(new DocRecord($@"C:\junk\zzzreportfiller{i}.txt",
                $"zzzreportfiller{i}.txt", 0, DateTime.UtcNow));

        // The high-quality match added LAST (highest doc ID — would be dropped
        // by the buggy truncate-then-sort path).
        records.Add(new DocRecord(@"C:\root\report.txt", "report.txt", 0, DateTime.UtcNow));

        idx.ReplaceAll(records);

        var hits = idx.Search("report", maxResults: 5, scopePaths: []);

        Assert.AreEqual(5, hits.Count);
        Assert.AreEqual("report.txt", hits[0].FileName,
            "high-scoring match must survive truncation, not be dropped because of late doc-ID");
        Assert.AreEqual(0.9f, hits[0].Score, "filename starts with query → 0.9 tier");
        // All other hits should be the lower-scoring filler matches.
        foreach (var h in hits.Skip(1))
            Assert.IsTrue(h.Score < hits[0].Score,
                $"filler '{h.FileName}' (score {h.Score}) should rank below top match");
    }

    [TestMethod]
    public void Scoring_TwoTierRefinement_UpgradesQuickScoreToPrecise()
    {
        // QuickScore (the bulk pass) only knows exact / startsWith / contains.
        // The precise Scorer adds camelCase-prefix detection (0.6 tier) which
        // ranks above plain substring (0.4). The refinement pass must surface
        // that, otherwise the top-N order is wrong.
        //
        // "ann" is NOT a startsWith for either file (both start with "notes")
        // and IS a substring of both filenames — so QuickScore returns 0.4 for
        // both. But the precise Scorer sees "ann" is a prefix of file-token
        // "annual" while "ann" only appears mid-string in "scanner" → the
        // first file should rank above the second after refinement.
        var idx = BuildIndex(
            (@"C:\root\notes_annual.pdf", "notes_annual.pdf"),
            (@"C:\root\notes_scanner.pdf", "notes_scanner.pdf"));

        var hits = idx.Search("ann", maxResults: 10, scopePaths: []);

        Assert.AreEqual(2, hits.Count);
        Assert.AreEqual("notes_annual.pdf", hits[0].FileName,
            "camelCase-prefix match must rank above plain-substring after refinement");
        Assert.IsTrue(hits[0].Score > hits[1].Score,
            $"prefix tier (0.6) must beat substring tier (0.4); got {hits[0].Score} vs {hits[1].Score}");
    }

    [TestMethod]
    public void Scoring_PrefixOnFilename_RanksAboveTokenMatch()
    {
        // A file whose name starts with the query should rank above a file
        // where the query is just an interior token. Both go through the
        // index hit path; only the precise score distinguishes them.
        var idx = BuildIndex(
            (@"C:\root\report.txt", "report.txt"),          // 0.9: starts with "report"
            (@"C:\root\my_report.txt", "my_report.txt"));   // 0.8: "report" is a token

        var hits = idx.Search("report", maxResults: 10, scopePaths: []);

        Assert.AreEqual(2, hits.Count);
        Assert.AreEqual("report.txt", hits[0].FileName);
        Assert.IsTrue(hits[0].Score > hits[1].Score);
    }

    // ---- Trigram / mid-string substring search ----------------------------

    [TestMethod]
    public void Trigram_MidStringQuery_FindsFile()
    {
        // "phab" is not a token of "ALPHABET.md" but is a mid-string substring.
        var idx = BuildIndex(
            (@"C:\root\ALPHABET.md", "ALPHABET.md"),
            (@"C:\root\unrelated.txt", "unrelated.txt"));

        var hits = Search(idx, "phab");

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual("ALPHABET.md", hits[0].FileName);
    }

    [TestMethod]
    public void Trigram_PrefixQuery_StillFindsFile()
    {
        // Trigram search should not break whole-word prefix queries.
        var idx = BuildIndex(
            (@"C:\root\alphabet.txt", "alphabet.txt"),
            (@"C:\root\unrelated.txt", "unrelated.txt"));

        var hits = Search(idx, "alpha");

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual("alphabet.txt", hits[0].FileName);
    }

    [TestMethod]
    public void Trigram_MultiFileMatches_AllReturned()
    {
        var idx = BuildIndex(
            (@"C:\root\reporting.pdf", "reporting.pdf"),
            (@"C:\root\report.txt", "report.txt"),
            (@"C:\root\prereport.docx", "prereport.docx"),
            (@"C:\root\unrelated.log", "unrelated.log"));

        // "epor" is mid-string in all three "report" variants but not in "unrelated".
        var hits = Search(idx, "epor");
        var names = hits.Select(h => h.FileName).ToHashSet();

        Assert.IsTrue(names.Contains("reporting.pdf"));
        Assert.IsTrue(names.Contains("report.txt"));
        Assert.IsTrue(names.Contains("prereport.docx"));
        Assert.IsFalse(names.Contains("unrelated.log"));
    }

    [TestMethod]
    public void Trigram_NoMatch_ReturnsEmpty()
    {
        var idx = BuildIndex((@"C:\root\document.txt", "document.txt"));
        Assert.AreEqual(0, Search(idx, "xyz").Count);
    }

    [TestMethod]
    public void Trigram_ShortQuery_TokenFallback()
    {
        // 2-char queries are below trigram threshold; token index still works.
        var idx = BuildIndex((@"C:\root\my_file.txt", "my_file.txt"));
        Assert.AreEqual(1, Search(idx, "my").Count);
    }

    [TestMethod]
    public void Trigram_Upsert_MidStringSearchable()
    {
        var idx = new FileIndex();
        idx.ReplaceAll([]);
        idx.Upsert(@"C:\root\ALPHABET.md", "ALPHABET.md", 0, DateTime.UtcNow);

        var hits = Search(idx, "phab");
        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual("ALPHABET.md", hits[0].FileName);
    }

    [TestMethod]
    public void Trigram_DeletedFile_NotReturnedForMidStringQuery()
    {
        var idx = BuildIndex((@"C:\root\ALPHABET.md", "ALPHABET.md"));
        idx.Delete(@"C:\root\ALPHABET.md");

        Assert.AreEqual(0, Search(idx, "phab").Count);
    }

    [TestMethod]
    public void Trigram_UnionWithTokenHits_NoDuplicates()
    {
        // "alpha" is both a whole token and a prefix of "alphabet" —
        // the result set should contain "alpha.txt" exactly once.
        var idx = BuildIndex((@"C:\root\alpha.txt", "alpha.txt"));

        var hits = Search(idx, "alpha");

        Assert.AreEqual(1, hits.Count);
    }

    [TestMethod]
    public void Trigram_CaseInsensitive_FindsFile()
    {
        var idx = BuildIndex((@"C:\root\UPPERCASE.txt", "UPPERCASE.txt"));

        // Trigrams are lowercased; query should match regardless of case.
        Assert.AreEqual(1, Search(idx, "PPER").Count);
        Assert.AreEqual(1, Search(idx, "pper").Count);
        Assert.AreEqual(1, Search(idx, "Pper").Count);
    }

    // ---- Corpus invariant --------------------------------------------------

    [TestMethod]
    [DataRow("report")]
    [DataRow("summary")]
    [DataRow("meeting")]
    [DataRow("config")]
    [DataRow("build")]
    public void CorpusInvariant_IndexedMatchesNaiveTokenSearch(string queryToken)
    {
        var files = new[]
        {
            "annual_report.pdf",
            "quarterly_report.docx",
            "meeting_notes.txt",
            "config_build.json",
            "build_output.log",
            "summary_q3.xlsx",
            "invoice.pdf",
            "unrelated.txt",
            "MyDocumentFinal.docx",
            "report_summary.md",
            "測試_report.txt",
        };

        const string root = @"C:\test";
        var idx = new FileIndex();
        var records = files
            .Select(f => new DocRecord(Path.Combine(root, f), f, 0, DateTime.UtcNow))
            .ToList();
        idx.ReplaceAll(records);

        // Naive oracle: files whose tokenized name contains the query token.
        var expected = files
            .Where(f => Tokenizer.Tokenize(f)
                .Any(t => t.Equals(queryToken, StringComparison.OrdinalIgnoreCase)))
            .Select(f => Path.Combine(root, f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var indexed = Search(idx, queryToken)
            .Select(h => h.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in expected)
            Assert.IsTrue(indexed.Contains(path), $"False negative: '{path}' missing from index results for query '{queryToken}'");

        foreach (var path in indexed)
            Assert.IsTrue(expected.Contains(path), $"False positive: '{path}' returned by index but not in naive oracle for query '{queryToken}'");
    }
}
