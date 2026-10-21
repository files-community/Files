// Copyright (c) Files Community
// Licensed under the MIT License.

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Files.App.Utils.FileTags
{
	public sealed class FileTagsDatabase
	{
		private readonly FileTagRegistryStore registry = new();

		public void SetTags(string filePath, ulong? frn, string[] tags) => registry.SetTags(filePath, frn, tags);

		public void UpdateTag(string oldFilePath, ulong? frn, string? newFilePath) => registry.UpdateTag(oldFilePath, frn, newFilePath);

		public void UpdateTag(ulong oldFrn, ulong? frn, string? newFilePath) => registry.UpdateTag(oldFrn, frn, newFilePath);

		public string[] GetTags(string? filePath, ulong? frn) => registry.GetTags(filePath, frn);

		public async Task<IEnumerable<TaggedFile>> GetAllAsync()
		{
			var items = await Server.AppService.GetTaggedFilesAsync();
			return items.Select(item => new TaggedFile { FilePath = item.Path, Frn = item.FileId, Tags = item.Tags }).ToArray();
		}

		public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath)
		{
			return GetAllUnderPathAsync(folderPath).GetAwaiter().GetResult();
		}

		public async Task<IEnumerable<TaggedFile>> GetAllUnderPathAsync(string folderPath)
		{
			var items = await GetAllAsync();
			if (string.IsNullOrEmpty(folderPath))
				return items;
			var folder = folderPath.Replace('/', '\\').TrimEnd('\\');
			var prefix = folder + "\\";
			return items.Where(item => item.FilePath.Equals(folder, StringComparison.OrdinalIgnoreCase)
				|| item.FilePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
		}

		public void Import(string json)
		{
			if (!registry.IsAvailable)
				return;

			var tags = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.TaggedFileArray);
			registry.ReplaceAll(tags);
		}

		public string Export()
		{
			return JsonSerializer.Serialize(registry.GetAll(), AppJsonSerializerContext.Default.ListTaggedFile);
		}
	}
}
