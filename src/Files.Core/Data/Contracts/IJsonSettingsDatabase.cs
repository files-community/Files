// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Contracts
{
	public interface IJsonSettingsDatabase
	{
		TValue? GetValue<TValue>(string key, TValue? defaultValue = default);

		bool SetValue<TValue>(string key, TValue? newValue);

		bool RemoveKey(string key);

		bool FlushSettings();

		bool ImportSettings(object? import);

		object? ExportSettings();

		bool CreateFile(string path);

		string ReadFromFile();

		bool WriteToFile(string? text);
	}
}
