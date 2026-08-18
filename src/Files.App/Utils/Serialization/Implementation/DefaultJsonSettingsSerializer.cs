// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
	{
		public static readonly JsonSerializerOptions Options = new JsonSerializerOptions(AotJson.Options)
		{
			WriteIndented = true
		};

		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = AotJson.SerializerTrimJustification)]
		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = AotJson.SerializerTrimJustification)]
		public string SerializeToJson(object? obj)
		{
			return JsonSerializer.Serialize(obj, Options);
		}

		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = AotJson.SerializerTrimJustification)]
		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = AotJson.SerializerTrimJustification)]
		public T? DeserializeFromJson<T>(string json)
		{
			return JsonSerializer.Deserialize<T?>(json, Options);
		}
	}
}
