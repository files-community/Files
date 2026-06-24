// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Files.SearchService.Usn;

/// <summary>
/// Enumerates every file on an NTFS volume via FSCTL_ENUM_USN_DATA.
/// Requires LocalSystem or SeBackupPrivilege — provided by the MSIX
/// service registration (StartAccount=localSystem).
/// Falls back to directory walking when the volume handle cannot be opened
/// (dev / non-elevated mode).
/// </summary>
internal sealed class UsnJournalReader
{
	private readonly string _root;

	public UsnJournalReader(string root) => _root = root;

	/// <summary>
	/// Yields (fullPath, fileName, sizeBytes, modifiedUtc) for every file under _root.
	/// </summary>
	public IEnumerable<FileEntry> Enumerate(CancellationToken cancellation = default)
	{
		var driveLetter = Path.GetPathRoot(_root) ?? _root;
		var volumePath  = $@"\\.\{driveLetter.TrimEnd('\\')}";

		SafeFileHandle? volume = null;
		try { volume = OpenVolumeHandle(volumePath); }
		catch { }

		if (volume is null || volume.IsInvalid)
		{
			foreach (var entry in FallbackWalk(_root, cancellation))
				yield return entry;
			yield break;
		}

		using (volume)
		{
			IEnumerable<FileEntry> entries;
			try   { entries = EnumerateViaUsn(volume, cancellation); }
			catch { entries = FallbackWalk(_root, cancellation); }

			foreach (var entry in entries)
				yield return entry;
		}
	}

	// --- USN path -----------------------------------------------------------

	private IEnumerable<FileEntry> EnumerateViaUsn(SafeFileHandle volume, CancellationToken cancellation)
	{
		ulong rootFrn;
		try   { rootFrn = GetRootFrn(_root); }
		catch { return FallbackWalk(_root, cancellation); }

		var (dirs, files) = ParseMft(volume, cancellation);

		var results = new ConcurrentBag<FileEntry>();

		// Use data already in the USN record — no per-file stat calls.
		// Size is stored as 0; the watcher fills it in accurately on the next
		// file-change event. Timestamp is the FILETIME of the last USN record
		// for that file, which is close enough to LastWriteTime for sorting.
		Parallel.ForEach(files, new ParallelOptions { CancellationToken = cancellation }, file =>
		{
			var path = ResolvePath(file.ParentFrn, file.Name, rootFrn, _root, dirs);
			if (path is null) return;

			var modifiedUtc = file.Timestamp > 0
				? DateTime.FromFileTimeUtc(file.Timestamp)
				: DateTime.UtcNow;

			results.Add(new FileEntry(path, file.Name, 0UL, modifiedUtc));
		});

		return results;
	}

	// --- MFT parsing --------------------------------------------------------

	private readonly record struct DirEntry(ulong ParentFrn, string Name);
	private readonly record struct FileRecord(ulong Frn, ulong ParentFrn, string Name, long Timestamp);

	private static (Dictionary<ulong, DirEntry> Dirs, List<FileRecord> Files) ParseMft(
		SafeFileHandle volume, CancellationToken cancellation)
	{
		const int BufferSize = 256 * 1024;
		var buffer = new byte[BufferSize];

		var dirs  = new Dictionary<ulong, DirEntry>();
		var files = new List<FileRecord>();

		var enumData = new NativeMethods.MFT_ENUM_DATA_V0
		{
			StartFileReferenceNumber = 0,
			LowUsn  = 0,
			HighUsn = long.MaxValue,
		};

		int inSize      = Marshal.SizeOf<NativeMethods.MFT_ENUM_DATA_V0>();
		int recordHdrSz = Marshal.SizeOf<NativeMethods.USN_RECORD_V2>();

		while (!cancellation.IsCancellationRequested)
		{
			bool ok = NativeMethods.DeviceIoControl(
				volume,
				NativeMethods.FSCTL_ENUM_USN_DATA,
				ref enumData,
				inSize,
				buffer,
				BufferSize,
				out int bytesReturned,
				nint.Zero);

			// bytesReturned == 8 means only the next-FRN cursor came back (no records left).
			// !ok covers ERROR_HANDLE_EOF and any other terminal error.
			if (!ok || bytesReturned <= 8) break;

			// First 8 bytes of output = next StartFileReferenceNumber.
			enumData.StartFileReferenceNumber = MemoryMarshal.Read<ulong>(buffer);

			int offset = 8;
			while (offset + recordHdrSz <= bytesReturned)
			{
				var rec = MemoryMarshal.Read<NativeMethods.USN_RECORD_V2>(buffer.AsSpan(offset));

				if (rec.RecordLength < recordHdrSz) break; // malformed — stop parsing this batch

				int nameStart = offset + rec.FileNameOffset;
				int nameEnd   = nameStart + rec.FileNameLength;

				if (nameEnd <= bytesReturned &&
				    (rec.FileAttributes & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) == 0)
				{
					var name      = Encoding.Unicode.GetString(buffer, nameStart, rec.FileNameLength);
					ulong frn     = rec.FileReferenceNumber       & NativeMethods.FRN_MFT_MASK;
					ulong parentFrn = rec.ParentFileReferenceNumber; // masked in ResolvePath

					if ((rec.FileAttributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0)
						dirs[frn] = new DirEntry(parentFrn, name);
					else
						files.Add(new FileRecord(frn, parentFrn, name, rec.TimeStamp));
				}

				offset += (int)rec.RecordLength;
			}
		}

		return (dirs, files);
	}

	// --- Path resolution ----------------------------------------------------

	/// <summary>
	/// Walks up the parent FRN chain from <paramref name="fileParentFrn"/> until
	/// it reaches <paramref name="rootFrn"/>, accumulating directory names.
	/// Returns null if the file is not under root or the chain is broken.
	/// </summary>
	private static string? ResolvePath(
		ulong fileParentFrn, string fileName, ulong rootFrn, string rootPath,
		Dictionary<ulong, DirEntry> dirs)
	{
		// Segments pushed in leaf-to-root order; Stack iterates top-to-bottom (root-to-leaf).
		var segments = new Stack<string>();
		ulong current = fileParentFrn & NativeMethods.FRN_MFT_MASK;

		for (int depth = 0; depth < 64; depth++)
		{
			if (current == rootFrn)
			{
				var path = rootPath;
				foreach (var seg in segments)          // top = nearest child of root
					path = Path.Combine(path, seg);
				return Path.Combine(path, fileName);
			}

			if (!dirs.TryGetValue(current, out var dir))
				return null; // chain broken or file not under root

			segments.Push(dir.Name);
			current = dir.ParentFrn & NativeMethods.FRN_MFT_MASK;
		}

		return null; // exceeded max depth — cycle guard
	}

	// --- Helpers ------------------------------------------------------------

	/// <summary>
	/// Returns the 48-bit MFT record number for <paramref name="path"/>.
	/// GetFileInformationByHandle returns only the record number (no sequence bits).
	/// </summary>
	private static ulong GetRootFrn(string path)
	{
		using var handle = NativeMethods.CreateFileW(
			path,
			NativeMethods.GENERIC_READ,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
			nint.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
			nint.Zero);

		if (handle.IsInvalid)
			throw new IOException($"Cannot open directory handle for '{path}'.");

		if (!NativeMethods.GetFileInformationByHandle(handle, out var info))
			throw new IOException($"GetFileInformationByHandle failed for '{path}'.");

		return ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
	}

	private static SafeFileHandle OpenVolumeHandle(string volumePath) =>
		NativeMethods.CreateFileW(
			volumePath,
			NativeMethods.GENERIC_READ,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
			nint.Zero,
			NativeMethods.OPEN_EXISTING,
			0,
			nint.Zero);

	// --- Fallback path (dev / non-elevated) ---------------------------------

	private static IEnumerable<FileEntry> FallbackWalk(string root, CancellationToken cancellation)
	{
		var opts = new EnumerationOptions
		{
			IgnoreInaccessible      = true,
			RecurseSubdirectories   = true,
			AttributesToSkip        = FileAttributes.ReparsePoint,
		};

		foreach (var fi in new DirectoryInfo(root).EnumerateFiles("*", opts))
		{
			cancellation.ThrowIfCancellationRequested();
			yield return new FileEntry(fi.FullName, fi.Name, (ulong)fi.Length, fi.LastWriteTimeUtc);
		}
	}
}

internal readonly record struct FileEntry(
	string   FullPath,
	string   FileName,
	ulong    SizeBytes,
	DateTime ModifiedUtc);
