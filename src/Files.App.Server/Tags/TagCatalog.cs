// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;

namespace Files.App.Server;

internal static class TagCatalog
{
	private static readonly FileTagRegistryStore database = new();
	private static readonly RegistryChangeMonitor monitor = new(Registry.CurrentUser,
		@$"Software\Files Community\{Windows.ApplicationModel.Package.Current.Id.Name}\v1\FileTags");
	internal static readonly CachedTask<IReadOnlyList<TaggedFileInfo>> Items = new(() => Task.FromResult<IReadOnlyList<TaggedFileInfo>>(
		database.GetAll(ignoreAccessErrors: true).Select(item => new TaggedFileInfo(item)).ToList().AsReadOnly()), TimeSpan.FromMinutes(5), monitor.HasChanged);
	private static readonly CachedTask<bool> maintenance = new(() => Task.Run(() =>
	{
		Reconcile();
		Items.Invalidate();
		return true;
	}), TimeSpan.FromMinutes(5));

	internal static Task InitializeAsync() => maintenance.GetAsync();

	private static string[]? ReadFileTag(string path)
	{
		try
		{
			using var stream = new FileStream($"{path}:files", FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd().Split(',', StringSplitOptions.RemoveEmptyEntries);
		}
		catch (IOException ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
		{
			// Keep tags on unavailable volumes or shares; remove confirmed missing files and streams.
			return File.Exists(path) || Directory.Exists(path) || Directory.Exists(Path.GetPathRoot(path)) ? [] : null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	private static void Reconcile()
	{
		foreach (var file in database.GetAll(ignoreAccessErrors: true))
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				var resolved = FileIdentity.PathFromFileId(file.Frn ?? 0, file.FilePath)?.Replace(@"\\?\", "", StringComparison.Ordinal);
				var path = resolved ?? file.FilePath;
				var tags = ReadFileTag(path);
				if (tags is null)
					return;
				var id = file.Frn;
				if (tags.Length > 0)
				{
					if (resolved is not null)
						database.UpdateTag(id ?? 0, null, path);
					else
					{
						id = FileIdentity.GetFileFRN(path);
						database.UpdateTag(file.FilePath, id, null);
					}
				}
				database.SetTags(path, id, tags);
			}, Program.Logger);
		}
	}

}
