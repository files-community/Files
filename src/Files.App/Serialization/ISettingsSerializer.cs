namespace Files.App.Serialization
{
	internal interface ISettingsSerializer
	{
		bool CreateFile(string path);

		string ReadFromFile();

		bool WriteToFile(string? text);
	}
}
