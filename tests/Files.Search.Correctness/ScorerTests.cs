// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Index;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.Search.Correctness;

[TestClass]
public class ScorerTests
{
    private static float Score(string query, string fileName)
    {
        var tokens = Tokenizer.Tokenize(query).ToList();
        return Scorer.Score(query, tokens, fileName);
    }

    // ---- Tier 1.0 — exact filename match -----------------------------------

    [TestMethod]
    public void Exact_CaseInsensitive_ReturnsOne()
    {
        Assert.AreEqual(1.0f, Score("report.txt", "report.txt"));
        Assert.AreEqual(1.0f, Score("REPORT.TXT", "report.txt"));
        Assert.AreEqual(1.0f, Score("report.txt", "REPORT.TXT"));
    }

    // ---- Tier 0.9 — filename starts with query -----------------------------

    [TestMethod]
    public void Prefix_ReturnsNinetyPercent()
    {
        // "report" is a prefix of "report.txt"
        Assert.AreEqual(0.9f, Score("report", "report.txt"));
    }

    [TestMethod]
    public void Prefix_PartialWord()
    {
        // "rep" is a prefix of "report.txt"
        Assert.AreEqual(0.9f, Score("rep", "report.txt"));
    }

    // ---- Tier 0.8 — all query tokens exactly match filename tokens ---------

    [TestMethod]
    public void AllTokenExact_ReturnsEightyPercent()
    {
        // query "annual report" → tokens ["annual","report"]
        // file "annual_report.pdf" → tokens ["annual","report","pdf"]
        // all query tokens are exact file tokens → 0.8
        Assert.AreEqual(0.8f, Score("annual report", "annual_report.pdf"));
    }

    [TestMethod]
    public void AllTokenExact_MultiWord()
    {
        Assert.AreEqual(0.8f, Score("meeting notes", "meeting_notes.docx"));
    }

    // ---- Tier 0.6 — all query tokens are prefix of some filename token -----

    [TestMethod]
    public void AllTokenPrefix_ReturnsSixtyPercent()
    {
        // query "ann" → token ["ann"]
        // file "notes_annual.pdf" → tokens ["notes","annual","pdf"]
        // "ann" is a prefix of "annual" but "notes_annual.pdf" does NOT start with "ann" → 0.6
        var score = Score("ann", "notes_annual.pdf");
        Assert.AreEqual(0.6f, score);
    }

    // ---- Tier 0.4 — query tokens appear as substring in filename -----------
    // This tier is mainly a safety net; in normal index operation a doc
    // can only reach the scorer if all query tokens are exact index tokens,
    // which means AllTokenExact (0.8) or AllTokenPrefix (0.6) will fire first.
    // Test it via direct Scorer.Score call to verify the tier exists and works.

    [TestMethod]
    public void AllSubstring_ReturnsFortyPercent()
    {
        // Contrived case: query "nual" is a mid-string match only.
        // 0.9: "annual_report.pdf" does NOT start with "nual"
        // 0.8: "nual" is NOT an exact file token
        // 0.6: "nual" is NOT a prefix of any file token ("annual", "report", "pdf")
        // 0.4: "nual" IS a substring of "annual_report.pdf"
        Assert.AreEqual(0.4f, Score("nual", "annual_report.pdf"));
    }

    // ---- Score ordering ----------------------------------------------------

    [TestMethod]
    public void ExactBeatsPrefix()
    {
        Assert.IsTrue(Score("report.txt", "report.txt") > Score("report", "report.txt"));
    }

    [TestMethod]
    public void PrefixBeatsAllTokenExact()
    {
        Assert.IsTrue(Score("report", "report.txt") > Score("annual report", "annual_report.pdf"));
    }

    [TestMethod]
    public void AllTokenExactBeatsAllTokenPrefix()
    {
        Assert.IsTrue(Score("annual report", "annual_report.pdf") > Score("ann rep", "annual_report.pdf"));
    }
}
