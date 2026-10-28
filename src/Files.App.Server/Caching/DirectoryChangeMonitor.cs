// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO.Enumeration;

namespace Files.App.Server;

internal sealed partial class DirectoryChangeMonitor : IDisposable
{
	private readonly Lock gate = new();
	private readonly Lock configureGate = new();
	private readonly string directory;
	private readonly string filter;
	private readonly bool recursive;
	private readonly Action<FileSystemEventArgs> changed;
	private FileSystemWatcher? watcher;
	private FileSystemWatcher? parentWatcher;
	private bool watchingTarget;
	private bool disposed;
	private long configurationVersion;

	internal DirectoryChangeMonitor(string directory, string filter, bool recursive, Action<FileSystemEventArgs> changed)
	{
		this.directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
		this.filter = filter;
		this.recursive = recursive;
		this.changed = changed;
		Configure();
	}

	private void Configure()
	{
		long version;
		lock (gate)
		{
			if (disposed)
				return;
			version = ++configurationVersion;
		}
		var existing = directory;
		var next = directory;
		while (!Directory.Exists(existing))
		{
			next = existing;
			existing = Path.GetDirectoryName(existing);
			if (string.IsNullOrEmpty(existing))
				return;
		}

		var targetExists = string.Equals(existing, directory, StringComparison.OrdinalIgnoreCase);
		var replacement = CreateWatcher(existing, targetExists ? filter : Path.GetFileName(next), targetExists && recursive,
			NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite);
		FileSystemWatcher? replacementParent = null;
		try
		{
			if (targetExists && Path.GetDirectoryName(directory) is { } parent)
				replacementParent = CreateWatcher(parent, Path.GetFileName(directory), false, NotifyFilters.DirectoryName);
		}
		catch
		{
			replacement.Dispose();
			throw;
		}
		FileSystemWatcher? previous;
		FileSystemWatcher? previousParent;
		lock (gate)
		{
			if (disposed || version != configurationVersion)
			{
				previous = replacement;
				previousParent = replacementParent;
			}
			else
			{
				previous = watcher;
				previousParent = parentWatcher;
				watcher = replacement;
				parentWatcher = replacementParent;
				watchingTarget = targetExists;
			}
		}
		previous?.Dispose();
		previousParent?.Dispose();
		// The missing component may have appeared before its parent watch was enabled.
		if (!targetExists && Directory.Exists(next))
			Configure();
	}

	private FileSystemWatcher CreateWatcher(string path, string pattern, bool includeSubdirectories, NotifyFilters notifyFilter)
	{
		var replacement = new FileSystemWatcher(path, pattern)
		{
			IncludeSubdirectories = includeSubdirectories,
			NotifyFilter = notifyFilter,
		};
		try
		{
			replacement.Created += OnChanged;
			replacement.Deleted += OnChanged;
			replacement.Changed += OnChanged;
			replacement.Renamed += OnChanged;
			replacement.Error += OnError;
			replacement.EnableRaisingEvents = true;
		}
		catch
		{
			replacement.Dispose();
			throw;
		}
		return replacement;
	}

	private bool Relevant(string path)
		=> (recursive
			? path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
			: string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase))
			&& FileSystemName.MatchesSimpleExpression(filter, Path.GetFileName(path), ignoreCase: true);

	private void OnChanged(object sender, FileSystemEventArgs args)
	{
		bool reconfigure;
		lock (gate)
		{
			if (disposed || (!ReferenceEquals(sender, watcher) && !ReferenceEquals(sender, parentWatcher)))
				return;
			reconfigure = ReferenceEquals(sender, parentWatcher) || !watchingTarget;
		}
		if (reconfigure || !Directory.Exists(directory))
		{
			if (!Reconfigure(sender))
				return;
			args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, string.Empty);
		}
		else if (!Relevant(args.FullPath) && !(args is RenamedEventArgs renamed && Relevant(renamed.OldFullPath)))
			return;
		if (!Volatile.Read(ref disposed))
			changed(args);
	}

	private void OnError(object sender, ErrorEventArgs args)
	{
		lock (gate)
		{
			if (disposed || (!ReferenceEquals(sender, watcher) && !ReferenceEquals(sender, parentWatcher)))
				return;
		}
		if (args.GetException() is not InternalBufferOverflowException && !Reconfigure(sender))
			return;
		if (!Volatile.Read(ref disposed))
			changed(new FileSystemEventArgs(WatcherChangeTypes.All, directory, string.Empty));
	}

	private bool Reconfigure(object sender)
	{
		lock (configureGate)
		{
			lock (gate)
			{
				if (disposed || (!ReferenceEquals(sender, watcher) && !ReferenceEquals(sender, parentWatcher)))
					return false;
			}
			try
			{
				Configure();
			}
			catch (Exception ex)
			{
				Program.Logger.LogWarning(ex, "Could not update a directory watch.");
			}
			return !Volatile.Read(ref disposed);
		}
	}

	public void Dispose()
	{
		FileSystemWatcher? previous;
		FileSystemWatcher? previousParent;
		lock (gate)
		{
			disposed = true;
			configurationVersion++;
			previous = watcher;
			previousParent = parentWatcher;
			watcher = null;
			parentWatcher = null;
		}
		previous?.Dispose();
		previousParent?.Dispose();
	}
}
