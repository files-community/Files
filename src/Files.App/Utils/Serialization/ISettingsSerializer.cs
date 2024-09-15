// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Serialization
{
	internal interface ISettingsSerializer
	{
		bool CreateFile(string path);

		string ReadFromFile();

		bool WriteToFile(string? text);
	}
}
