using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Files.Search.Corpora;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var opts = CliOptions.Parse(args);
            if (opts is null) return 1;

            if (Directory.Exists(opts.OutDir) && Directory.EnumerateFileSystemEntries(opts.OutDir).Any() && !opts.Force)
            {
                Console.Error.WriteLine($"error: --out '{opts.OutDir}' is not empty (use --force to overwrite)");
                return 2;
            }
            Directory.CreateDirectory(opts.OutDir);

            var sw = Stopwatch.StartNew();
            var manifest = Generator.Generate(opts);
            sw.Stop();
            manifest.GenerationSeconds = sw.Elapsed.TotalSeconds;

            var manifestPath = Path.Combine(opts.OutDir, "manifest.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ManifestJson.Default.Manifest));
            Console.WriteLine($"done: {manifest.FileCount:N0} files, {manifest.TotalBytes / (1024.0 * 1024 * 1024):F2} GiB, {sw.Elapsed.TotalSeconds:F1}s");
            Console.WriteLine($"manifest: {manifestPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }
}

internal sealed class CliOptions
{
    public required string OutDir { get; init; }
    public required string PresetName { get; init; }
    public required int FileCount { get; init; }
    public required long AvgFileBytes { get; init; }
    public required int Seed { get; init; }
    public bool Force { get; init; }

    public static CliOptions? Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            PrintUsage();
            return null;
        }

        string? outDir = null;
        string? preset = null;
        int? files = null;
        long? avgBytes = null;
        int seed = 42;
        bool force = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out": outDir = args[++i]; break;
                case "--preset": preset = args[++i]; break;
                case "--files": files = int.Parse(args[++i]); break;
                case "--avg-size": avgBytes = long.Parse(args[++i]); break;
                case "--seed": seed = int.Parse(args[++i]); break;
                case "--force": force = true; break;
                default: throw new ArgumentException($"unknown arg: {args[i]}");
            }
        }

        if (outDir is null) { Console.Error.WriteLine("error: --out is required"); return null; }

        // Presets — small targets a quick local run; medium/large need real disk.
        (string name, int count, long avg) = preset switch
        {
            "small"  => ("small",  50_000,    40L * 1024),       // ~2 GiB
            "medium" => ("medium", 500_000,   100L * 1024),      // ~50 GiB
            "large"  => ("large",  2_000_000, 250L * 1024),      // ~500 GiB
            null     => ("custom", files ?? throw new ArgumentException("--preset or --files required"),
                                   avgBytes ?? 40L * 1024),
            _        => throw new ArgumentException($"unknown preset: {preset}")
        };

        return new CliOptions
        {
            OutDir = Path.GetFullPath(outDir),
            PresetName = name,
            FileCount = count,
            AvgFileBytes = avg,
            Seed = seed,
            Force = force,
        };
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            files-corpora --out <dir> [--preset small|medium|large] [--files N] [--avg-size BYTES] [--seed N] [--force]

            Generates a deterministic file corpus for search benchmarking. Same seed + preset → identical tree.
            Writes manifest.json describing what was produced (and what queries can deterministically expect).
            """);
    }
}

internal static class Generator
{
    // Realistic-ish weighted extension mix.
    private static readonly (string ext, int weight, bool textLike)[] Extensions =
    [
        (".txt",  10, true),  (".md",   8,  true),  (".cs",   6, true),  (".json", 5, true),
        (".log",  6,  true),  (".html", 3,  true),  (".xml",  3, true),  (".csv",  3, true),
        (".docx", 6,  false), (".pdf",  6,  false), (".xlsx", 3, false), (".pptx", 2, false),
        (".jpg",  10, false), (".png",  6,  false), (".mp4",  3, false), (".zip",  4, false),
        (".dll",  4,  false), (".exe",  2,  false),
    ];

    private static readonly string[] WordPool =
    [
        "report","summary","invoice","draft","final","review","notes","meeting","backup","archive",
        "project","module","service","client","server","data","config","setup","build","release",
        "alpha","beta","gamma","delta","north","south","east","west","spring","summer","autumn","winter",
        "blue","red","green","orange","purple","silver","gold","copper","iron","quartz",
        "annual","quarterly","monthly","daily","internal","public","private","secure","draft","final",
    ];

    // "Needle" tokens placed deterministically so content-search benches can assert exact counts.
    public static readonly string[] NeedleTokens = ["xqz_alpha", "xqz_beta", "xqz_gamma", "xqz_delta"];

    public static Manifest Generate(CliOptions opts)
    {
        var rng = new Xorshift64(unchecked((ulong)opts.Seed * 0x9E3779B97F4A7C15UL + 1));
        long totalWeight = Extensions.Sum(e => e.weight);

        var manifest = new Manifest
        {
            Preset = opts.PresetName,
            Seed = opts.Seed,
            Root = opts.OutDir,
            FileCount = opts.FileCount,
            NeedleTokens = NeedleTokens,
            NeedleCounts = new Dictionary<string, int>(),
        };
        foreach (var t in NeedleTokens) manifest.NeedleCounts[t] = 0;

        // Pre-create a directory tree shaped roughly like a user data folder:
        // depth 0..4, branching ~6 at root, ~4 mid, ~2 deep.
        var dirs = BuildDirTree(opts.OutDir, rng, opts.FileCount);
        manifest.DirCount = dirs.Count;

        long bytes = 0;
        var sb = new StringBuilder(8 * 1024);
        var contentBuf = new byte[Math.Min(opts.AvgFileBytes * 4, 4 * 1024 * 1024)];
        int unicodeCount = 0, longPathCount = 0, hiddenCount = 0, zeroByteCount = 0;

        for (int i = 0; i < opts.FileCount; i++)
        {
            // Pick an extension by weight.
            long roll = (long)(rng.NextDouble() * totalWeight);
            string ext = ".txt"; bool textLike = true;
            long acc = 0;
            foreach (var e in Extensions) { acc += e.weight; if (roll < acc) { ext = e.ext; textLike = e.textLike; break; } }

            // Name (occasionally unicode / long).
            string baseName = MakeName(rng, sb);
            bool unicode = rng.NextDouble() < 0.01;
            bool longName = rng.NextDouble() < 0.005;
            if (unicode) { baseName = "测试_" + baseName + "_😀"; unicodeCount++; }
            if (longName) { baseName = baseName + new string('x', 180); longPathCount++; }
            string fileName = baseName + ext;

            string dir = dirs[(int)(rng.NextU64() % (ulong)dirs.Count)];
            string path = Path.Combine(dir, fileName);

            // Size: log-normal-ish around avg, clamped.
            double mult = Math.Pow(10, (rng.NextDouble() - 0.5) * 1.4); // ~0.04x..25x
            long size = Math.Max(0, (long)(opts.AvgFileBytes * mult));
            if (rng.NextDouble() < 0.002) { size = 0; zeroByteCount++; }
            if (size > contentBuf.Length) size = contentBuf.Length;

            try
            {
                if (textLike && size > 0)
                {
                    int needles = WriteText(contentBuf, (int)size, rng, manifest.NeedleCounts);
                    File.WriteAllBytes(path, contentBuf.AsSpan(0, (int)size).ToArray());
                }
                else
                {
                    rng.NextBytes(contentBuf.AsSpan(0, (int)size));
                    File.WriteAllBytes(path, size == 0 ? Array.Empty<byte>() : contentBuf.AsSpan(0, (int)size).ToArray());
                }
                bytes += size;

                // ~1% hidden.
                if (rng.NextDouble() < 0.01)
                {
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
                    hiddenCount++;
                }
            }
            catch (PathTooLongException) { longPathCount--; /* silently drop */ }
            catch (IOException) { /* tolerate transient issues */ }

            if ((i & 0xFFF) == 0 && i > 0)
                Console.Write($"\r  {i:N0} / {opts.FileCount:N0} files");
        }
        Console.WriteLine($"\r  {opts.FileCount:N0} / {opts.FileCount:N0} files");

        manifest.TotalBytes = bytes;
        manifest.UnicodeNameCount = unicodeCount;
        manifest.LongPathCount = longPathCount;
        manifest.HiddenCount = hiddenCount;
        manifest.ZeroByteCount = zeroByteCount;
        return manifest;
    }

    private static List<string> BuildDirTree(string root, Xorshift64 rng, int fileCount)
    {
        // Aim for ~50 files per leaf dir on average.
        int leafCount = Math.Max(1, fileCount / 50);
        var dirs = new List<string> { root };
        var queue = new Queue<(string path, int depth)>();
        queue.Enqueue((root, 0));
        while (dirs.Count < leafCount && queue.Count > 0)
        {
            var (p, d) = queue.Dequeue();
            int branch = d == 0 ? 6 : d <= 2 ? 4 : 2;
            for (int i = 0; i < branch && dirs.Count < leafCount; i++)
            {
                string sub = Path.Combine(p, $"d{d}_{rng.NextU64() % 10000:0000}");
                Directory.CreateDirectory(sub);
                dirs.Add(sub);
                if (d < 4) queue.Enqueue((sub, d + 1));
            }
        }
        return dirs;
    }

    private static string MakeName(Xorshift64 rng, StringBuilder sb)
    {
        sb.Clear();
        int parts = 1 + (int)(rng.NextU64() % 3);
        for (int i = 0; i < parts; i++)
        {
            if (i > 0) sb.Append('_');
            sb.Append(WordPool[(int)(rng.NextU64() % (ulong)WordPool.Length)]);
        }
        sb.Append('_').Append(rng.NextU64() % 1_000_000);
        return sb.ToString();
    }

    private static int WriteText(byte[] buf, int size, Xorshift64 rng, Dictionary<string, int> needleCounts)
    {
        int written = 0;
        int needles = 0;
        var sb = new StringBuilder(256);
        while (written < size)
        {
            sb.Clear();
            int wordsThisLine = 6 + (int)(rng.NextU64() % 12);
            for (int w = 0; w < wordsThisLine; w++)
            {
                if (w > 0) sb.Append(' ');
                // ~0.05% chance per word slot to plant a needle.
                if (rng.NextDouble() < 0.0005)
                {
                    var n = NeedleTokens[(int)(rng.NextU64() % (ulong)NeedleTokens.Length)];
                    sb.Append(n);
                    lock (needleCounts) needleCounts[n] = needleCounts[n] + 1;
                    needles++;
                }
                else
                {
                    sb.Append(WordPool[(int)(rng.NextU64() % (ulong)WordPool.Length)]);
                }
            }
            sb.Append('\n');
            int byteCount = Encoding.UTF8.GetByteCount(sb.ToString().AsSpan());
            if (written + byteCount > size) byteCount = size - written;
            if (byteCount <= 0) break;
            var slice = Encoding.UTF8.GetBytes(sb.ToString());
            Array.Copy(slice, 0, buf, written, Math.Min(byteCount, slice.Length));
            written += Math.Min(byteCount, slice.Length);
        }
        return needles;
    }
}

// Deterministic RNG — xorshift64*. Single-threaded; no hidden state.
internal sealed class Xorshift64
{
    private ulong _s;
    public Xorshift64(ulong seed) { _s = seed == 0 ? 0xDEADBEEFCAFEBABEUL : seed; }
    public ulong NextU64()
    {
        _s ^= _s >> 12; _s ^= _s << 25; _s ^= _s >> 27;
        return _s * 0x2545F4914F6CDD1DUL;
    }
    public double NextDouble() => (NextU64() >> 11) * (1.0 / (1UL << 53));
    public void NextBytes(Span<byte> dest)
    {
        int i = 0;
        while (i + 8 <= dest.Length)
        {
            ulong v = NextU64();
            for (int b = 0; b < 8; b++) dest[i + b] = (byte)(v >> (b * 8));
            i += 8;
        }
        if (i < dest.Length)
        {
            ulong v = NextU64();
            for (; i < dest.Length; i++) { dest[i] = (byte)v; v >>= 8; }
        }
    }
}

internal sealed class Manifest
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; init; } = 1;
    [JsonPropertyName("preset")] public string Preset { get; init; } = "";
    [JsonPropertyName("seed")] public int Seed { get; init; }
    [JsonPropertyName("root")] public string Root { get; init; } = "";
    [JsonPropertyName("fileCount")] public int FileCount { get; set; }
    [JsonPropertyName("dirCount")] public int DirCount { get; set; }
    [JsonPropertyName("totalBytes")] public long TotalBytes { get; set; }
    [JsonPropertyName("unicodeNameCount")] public int UnicodeNameCount { get; set; }
    [JsonPropertyName("longPathCount")] public int LongPathCount { get; set; }
    [JsonPropertyName("hiddenCount")] public int HiddenCount { get; set; }
    [JsonPropertyName("zeroByteCount")] public int ZeroByteCount { get; set; }
    [JsonPropertyName("needleTokens")] public string[] NeedleTokens { get; init; } = [];
    [JsonPropertyName("needleCounts")] public Dictionary<string, int> NeedleCounts { get; init; } = new();
    [JsonPropertyName("generationSeconds")] public double GenerationSeconds { get; set; }
}

[JsonSerializable(typeof(Manifest))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class ManifestJson : JsonSerializerContext { }
