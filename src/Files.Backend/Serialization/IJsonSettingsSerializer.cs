namespace Files.App.Serialization
{
	internal interface IJsonSettingsSerializer
	{
		string? SerializeToJson(object? obj);

		T? DeserializeFromJson<T>(string json);
	}
}
