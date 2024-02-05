// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Contracts
{
	public interface ISettingsSerializer
	{
		bool CreateFile(string path);

		string ReadFromFile();

		bool WriteToFile(string? text);
	}
}
