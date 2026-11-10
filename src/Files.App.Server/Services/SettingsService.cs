// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;

namespace Files.App.Server;

public sealed partial class SettingsService
{
	private SettingsService() { }

	public static SettingsValue[] GetSettings(string fileName) => SettingsStore.Get(fileName).ReadValues();

	public static bool SetSetting(string fileName, string key, [ReadOnlyArray] SettingsValue[] value) => SettingsStore.Get(fileName).Set(key, value, false);

	public static bool RemoveSetting(string fileName, string key) => SettingsStore.Get(fileName).Set(key, [], true);

	public static bool FlushSettings(string fileName) => SettingsStore.Get(fileName).Flush();

	public static bool FlushAllSettings() => SettingsStore.FlushAll();

	public static bool ImportSettings(string fileName, [ReadOnlyArray] SettingsValue[] values) => SettingsStore.Get(fileName).Import(values);
}
