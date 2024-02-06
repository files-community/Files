// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Contracts
{
	/// <summary>
	/// Represents contract class for json settings database.
	/// </summary>
	public interface IJsonSettingsDatabaseService
	{
		TValue? GetValue<TValue>(string key, TValue? defaultValue = default);

		bool SetValue<TValue>(string key, TValue? newValue);

		bool RemoveKey(string key);

		bool FlushSettings();

		bool ImportSettings(object? import);

		object? ExportSettings();

		bool CreateJsonFile(string path);

		string ReadJsonFile();

		bool WriteToJsonFile(string? text);
	}
}
