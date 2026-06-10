// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Index;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.Search.Correctness;

[TestClass]
public class PersistenceTests
{
    private string _tmpFile = "";

    [TestInitialize]
    public void Initialize()
    {
        _tmpFile = Path.Combine(Path.GetTempPath(), $"fsix_test_{Guid.NewGuid():N}.bin");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tmpFile)) File.Delete(_tmpFile);
        if (File.Exists(_tmpFile + ".tmp")) File.Delete(_tmpFile + ".tmp");
    }

    [TestMethod]
    public async Task RoundTrip_PreservesAllFields()
    {
        var utc = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var records = new List<DocRecord>
        {
            new(@"C:\root\report.pdf",  "report.pdf",  1024UL, utc),
            new(@"C:\root\notes.txt",   "notes.txt",   2048UL, utc.AddDays(1)),
        };

        await IndexPersistence.SaveAsync(_tmpFile, records, CancellationToken.None);
        var loaded = await IndexPersistence.LoadAsync(_tmpFile, CancellationToken.None);

        Assert.AreEqual(records.Count, loaded.Count);
        for (int i = 0; i < records.Count; i++)
        {
            Assert.AreEqual(records[i].FullPath,    loaded[i].FullPath);
            Assert.AreEqual(records[i].FileName,    loaded[i].FileName);
            Assert.AreEqual(records[i].SizeBytes,   loaded[i].SizeBytes);
            Assert.AreEqual(records[i].ModifiedUtc, loaded[i].ModifiedUtc);
        }
    }

    [TestMethod]
    public async Task RoundTrip_Unicode_PathAndFilename()
    {
        var records = new List<DocRecord>
        {
            new(@"C:\root\测试\测试_file.txt", "测试_file.txt", 512UL, DateTime.UtcNow),
        };

        await IndexPersistence.SaveAsync(_tmpFile, records, CancellationToken.None);
        var loaded = await IndexPersistence.LoadAsync(_tmpFile, CancellationToken.None);

        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(@"C:\root\测试\测试_file.txt", loaded[0].FullPath);
        Assert.AreEqual("测试_file.txt", loaded[0].FileName);
    }

    [TestMethod]
    public async Task RoundTrip_EmptyList()
    {
        await IndexPersistence.SaveAsync(_tmpFile, [], CancellationToken.None);
        var loaded = await IndexPersistence.LoadAsync(_tmpFile, CancellationToken.None);
        Assert.AreEqual(0, loaded.Count);
    }

    [TestMethod]
    public async Task RoundTrip_LargeCount_AllPresent()
    {
        const int count = 10_000;
        var utc = DateTime.UtcNow;
        var records = Enumerable.Range(0, count)
            .Select(i => new DocRecord($@"C:\root\file_{i}.txt", $"file_{i}.txt", (ulong)i, utc))
            .ToList();

        await IndexPersistence.SaveAsync(_tmpFile, records, CancellationToken.None);
        var loaded = await IndexPersistence.LoadAsync(_tmpFile, CancellationToken.None);

        Assert.AreEqual(count, loaded.Count);
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(records[i].FullPath,  loaded[i].FullPath);
            Assert.AreEqual(records[i].SizeBytes, loaded[i].SizeBytes);
        }
    }

    [TestMethod]
    public async Task SaveIsAtomic_TempFileCleanedUp()
    {
        await IndexPersistence.SaveAsync(_tmpFile, [], CancellationToken.None);
        // The .tmp file must be gone after a successful save.
        Assert.IsFalse(File.Exists(_tmpFile + ".tmp"));
    }

    [TestMethod]
    public async Task Load_CorruptedMagic_Throws()
    {
        // Write garbage bytes.
        await File.WriteAllBytesAsync(_tmpFile, [0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00]);
        bool threw = false;
        try { await IndexPersistence.LoadAsync(_tmpFile, CancellationToken.None); }
        catch (InvalidDataException) { threw = true; }
        Assert.IsTrue(threw, "Expected InvalidDataException was not thrown.");
    }

    [TestMethod]
    public async Task RoundTrip_LongPath_Preserved()
    {
        // Paths up to MAX_PATH-ish lengths should survive the round-trip.
        var longName = new string('x', 200) + ".txt";
        var longPath = @"C:\root\" + longName;
        var records = new List<DocRecord> { new(longPath, longName, 0UL, DateTime.UtcNow) };

        await IndexPersistence.SaveAsync(_tmpFile, records, CancellationToken.None);
        var loaded = await IndexPersistence.LoadAsync(_tmpFile, CancellationToken.None);

        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(longPath, loaded[0].FullPath);
        Assert.AreEqual(longName, loaded[0].FileName);
    }
}
