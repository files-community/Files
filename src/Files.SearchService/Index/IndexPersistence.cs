// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchService.Index;

/// <summary>
/// Persists and loads the doc store to/from a simple binary format.
/// Writes atomically (temp file + rename) to prevent corruption on
/// unclean shutdown.
///
/// Format:
///   [4 bytes] magic: 0x46534958 ("FSIX")
///   [4 bytes] version: 1
///   [8 bytes] record count
///   For each record:
///     [8 bytes] size_bytes
///     [8 bytes] modified_unix_ms
///     [4 bytes] full_path UTF-8 byte length
///     [N bytes] full_path UTF-8
///     [4 bytes] file_name UTF-8 byte length
///     [N bytes] file_name UTF-8
/// </summary>
internal static class IndexPersistence
{
	private const uint Magic = 0x46534958;
	private const int Version = 1;

	public static async Task SaveAsync(
		string path, IList<DocRecord> records, CancellationToken cancellation)
	{
		var tmp = path + ".tmp";
		await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
		await using (var bw = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: true))
		{
			bw.Write(Magic);
			bw.Write(Version);
			bw.Write((long)records.Count);

			foreach (var r in records)
			{
				cancellation.ThrowIfCancellationRequested();
				bw.Write(r.SizeBytes);
				bw.Write(r.ModifiedUtc.ToFileTimeUtc());
				WriteString(bw, r.FullPath);
				WriteString(bw, r.FileName);
			}
		}

		File.Move(tmp, path, overwrite: true);
	}

	public static async Task<List<DocRecord>> LoadAsync(
		string path, CancellationToken cancellation)
	{
		await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
		using var br = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);

		if (br.ReadUInt32() != Magic)
			throw new InvalidDataException("Index file has unexpected magic bytes — possible corruption.");
		if (br.ReadInt32() != Version)
			throw new InvalidDataException("Index file version mismatch — will rebuild.");

		var count = br.ReadInt64();
		var records = new List<DocRecord>((int)Math.Min(count, int.MaxValue));

		for (long i = 0; i < count; i++)
		{
			cancellation.ThrowIfCancellationRequested();
			var size = br.ReadUInt64();
			var modified = DateTime.FromFileTimeUtc(br.ReadInt64());
			var fullPath = ReadString(br);
			var fileName = ReadString(br);
			records.Add(new DocRecord(fullPath, fileName, size, modified));
		}

		return records;
	}

	private static void WriteString(BinaryWriter bw, string s)
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes(s);
		bw.Write(bytes.Length);
		bw.Write(bytes);
	}

	private static string ReadString(BinaryReader br)
	{
		var len = br.ReadInt32();
		var bytes = br.ReadBytes(len);
		return System.Text.Encoding.UTF8.GetString(bytes);
	}
}
