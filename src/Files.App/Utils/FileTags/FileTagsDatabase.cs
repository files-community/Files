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

		public IEnumerable<TaggedFile> GetAll() => registry.GetAll(ignoreAccessErrors: true);

		public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath) => registry.GetAllUnderPath(folderPath);

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
