// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Index;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.Search.Correctness;

[TestClass]
public class TokenizerTests
{
    private static HashSet<string> Tokens(string filename) =>
        Tokenizer.Tokenize(filename).ToHashSet(StringComparer.OrdinalIgnoreCase);

    // ---- Delimiter splitting -----------------------------------------------

    [TestMethod]
    public void DotSplit_ProducesNameAndExtension()
    {
        var t = Tokens("report.txt");
        Assert.IsTrue(t.Contains("report"));
        Assert.IsTrue(t.Contains("txt"));
    }

    [TestMethod]
    public void UnderscoreSplit()
    {
        var t = Tokens("annual_report.pdf");
        Assert.IsTrue(t.Contains("annual"));
        Assert.IsTrue(t.Contains("report"));
        Assert.IsTrue(t.Contains("pdf"));
    }

    [TestMethod]
    public void HyphenSplit()
    {
        var t = Tokens("my-document.txt");
        Assert.IsTrue(t.Contains("my"));
        Assert.IsTrue(t.Contains("document"));
    }

    [TestMethod]
    public void SpaceSplit()
    {
        var t = Tokens("meeting notes.docx");
        Assert.IsTrue(t.Contains("meeting"));
        Assert.IsTrue(t.Contains("notes"));
    }

    [TestMethod]
    public void MultiExtension()
    {
        var t = Tokens("archive.tar.gz");
        Assert.IsTrue(t.Contains("archive"));
        Assert.IsTrue(t.Contains("tar"));
        Assert.IsTrue(t.Contains("gz"));
    }

    [TestMethod]
    public void NumbersPreservedAsToken()
    {
        var t = Tokens("report_2024.pdf");
        Assert.IsTrue(t.Contains("2024"));
    }

    // ---- CamelCase splitting -----------------------------------------------

    [TestMethod]
    public void CamelCase_LowerUpper_Splits()
    {
        var t = Tokens("MyDocument.docx");
        Assert.IsTrue(t.Contains("my"));
        Assert.IsTrue(t.Contains("document"));
    }

    [TestMethod]
    public void CamelCase_MultipleWords()
    {
        var t = Tokens("AnnualReportFinal.pdf");
        Assert.IsTrue(t.Contains("annual"));
        Assert.IsTrue(t.Contains("report"));
        Assert.IsTrue(t.Contains("final"));
    }

    [TestMethod]
    public void LetterToDigit_Splits()
    {
        var t = Tokens("v2Final.docx");
        Assert.IsTrue(t.Contains("v"));
        Assert.IsTrue(t.Contains("2"));
        Assert.IsTrue(t.Contains("final"));
    }

    [TestMethod]
    public void DigitToLetter_Splits()
    {
        var t = Tokens("2024Report.pdf");
        Assert.IsTrue(t.Contains("2024"));
        Assert.IsTrue(t.Contains("report"));
    }

    [TestMethod]
    public void AllCaps_TreatedAsSingleToken()
    {
        var t = Tokens("REPORT.txt");
        Assert.IsTrue(t.Contains("report"));
    }

    // ---- Unicode -----------------------------------------------------------

    [TestMethod]
    public void Unicode_CJK_PreservedAsToken()
    {
        var t = Tokens("测试_file.txt");
        Assert.IsTrue(t.Contains("测试"));
        Assert.IsTrue(t.Contains("file"));
        Assert.IsTrue(t.Contains("txt"));
    }

    [TestMethod]
    public void Unicode_Emoji_DoesNotCrash()
    {
        var t = Tokens("测试_draft_😀.jpg");
        Assert.IsTrue(t.Contains("jpg"));
    }

    // ---- Edge cases --------------------------------------------------------

    [TestMethod]
    public void EmptyString_ReturnsNoTokens()
    {
        Assert.AreEqual(0, Tokenizer.Tokenize("").Count());
    }

    [TestMethod]
    public void OnlyDelimiters_ReturnsNoTokens()
    {
        Assert.AreEqual(0, Tokenizer.Tokenize("___...---").Count());
    }

    [TestMethod]
    public void AllTokensAreLowercase()
    {
        var tokens = Tokenizer.Tokenize("UPPER_lower_Mixed.TXT").ToList();
        foreach (var token in tokens)
            Assert.AreEqual(token.ToLowerInvariant(), token);
    }

    [TestMethod]
    public void ComplexFilename_ContainsExpectedTokens()
    {
        var t = Tokens("MyDocument_v2Final.docx");
        Assert.IsTrue(t.Contains("my"));
        Assert.IsTrue(t.Contains("document"));
        Assert.IsTrue(t.Contains("v"));
        Assert.IsTrue(t.Contains("2"));
        Assert.IsTrue(t.Contains("final"));
        Assert.IsTrue(t.Contains("docx"));
    }
}
